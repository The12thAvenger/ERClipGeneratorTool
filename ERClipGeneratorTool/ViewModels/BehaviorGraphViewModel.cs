using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using DynamicData;
using DynamicData.Binding;
using ERClipGeneratorTool.Models.TAE;
using ERClipGeneratorTool.Util;
using ERClipGeneratorTool.ViewModels.Interactions;
using HKLib.hk2018;
using ReactiveHistory;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SoulsFormats;

namespace ERClipGeneratorTool.ViewModels;

public class BehaviorGraphViewModel : ViewModelBase
{
    private readonly Dictionary<int, List<CustomManualSelectorGenerator>> _cmsgsByAnimId;
    private readonly IHistory _history;
    private ClipGeneratorViewModel? _currentClipGenerator;
    private short _nextAnimationInternalId;

    public BehaviorGraphViewModel(hkRootLevelContainer rootLevelContainer, List<IHavokObject> objects, IHistory history,
        FileSource fileSource)
    {
        _history = history;
        FileSource = fileSource;
        RootLevelContainer = rootLevelContainer;

        List<CustomManualSelectorGenerator> cmsgs = objects
            .Where(x => x is CustomManualSelectorGenerator cmsg
                        && cmsg.m_generators.Count > 0
                        && cmsg.m_generators[0] is hkbClipGenerator)
            .Cast<CustomManualSelectorGenerator>().ToList();

        Dictionary<hkbClipGenerator, List<CustomManualSelectorGenerator>> clipParents = objects
            .Where(x => x is hkbClipGenerator)
            .Cast<hkbClipGenerator>()
            .Distinct()
            .ToDictionary(x => x, _ => new List<CustomManualSelectorGenerator>());

        foreach (CustomManualSelectorGenerator cmsg in cmsgs)
        {
            foreach (hkbGenerator? generator in cmsg.m_generators)
            {
                if (generator is not hkbClipGenerator clipGenerator) continue;
                if (!clipParents.TryGetValue(clipGenerator, out List<CustomManualSelectorGenerator>? parents))
                {
                    parents = new List<CustomManualSelectorGenerator>();
                    clipParents.Add(clipGenerator, parents);
                }

                parents.Add(cmsg);
            }
        }

        ClipGenerators = new SourceCache<ClipGeneratorViewModel, string>(x => x.Name);
        ClipGenerators.AddOrUpdate(clipParents.Select(x => new ClipGeneratorViewModel(x.Key, x.Value, _history)));


        _nextAnimationInternalId = ClipGenerators.Items.Select(x => x.AnimationInternalId).Max();
        _nextAnimationInternalId++;

        _cmsgsByAnimId = cmsgs.GroupBy(x => x.m_animId).ToDictionary(x => x.Key, x => x.ToList());

        IObservable<Func<ClipGeneratorViewModel, bool>> generatorsFilter = this.WhenAnyValue(x => x.Filter)
            .Throttle(TimeSpan.FromMilliseconds(100))
            .Select(x => (Func<ClipGeneratorViewModel, bool>)(y => y.AnimationName.StartsWith(x)));

        FilteredGenerators = new ObservableCollectionExtended<ClipGeneratorViewModel>();
        ClipGenerators.Connect()
            .Filter(generatorsFilter)
            .SortBy(x => x.Name)
            .Bind(FilteredGenerators)
            .Subscribe();

        NewGeneratorCommand = ReactiveCommand.CreateFromTask(NewGeneratorAsync);

        IObservable<bool> isGeneratorSelected =
            this.WhenAnyValue(x => x.CurrentClipGenerator).Select(x => x is not null);
        DeleteCommand = ReactiveCommand.Create(DeleteCurrentGenerator, isGeneratorSelected);
    }

    public ObservableCollection<ClipGeneratorViewModel> SelectedClipGenerators { get; } = new();

    [Reactive] public string Filter { get; set; } = "";

    public FileSource FileSource { get; set; }

    public hkRootLevelContainer RootLevelContainer { get; }

    public SourceCache<ClipGeneratorViewModel, string> ClipGenerators { get; }

    public ObservableCollectionExtended<ClipGeneratorViewModel> FilteredGenerators { get; }

    public ClipGeneratorViewModel? CurrentClipGenerator
    {
        get => _currentClipGenerator;
        set
        {
            if (value is not null)
            {
                this.RaiseAndSetIfChanged(ref _currentClipGenerator, value);
            }
        }
    }

    // bypasses null check
    private ClipGeneratorViewModel? CurrentClipGeneratorInternal
    {
        get => _currentClipGenerator;
        set => this.RaiseAndSetIfChanged(ref _currentClipGenerator, value, nameof(CurrentClipGenerator));
    }

    public ReactiveCommand<Unit, Unit> NewGeneratorCommand { get; }

    public ReactiveCommand<Unit, Unit> DeleteCommand { get; }

    public Interaction<ClipGeneratorOptionsViewModel, bool> GetClipGeneratorOptions { get; } = new();
    public Interaction<ClipGeneratorDupeViewModel, bool> GetClipGeneratorDupeOptions { get; } = new();
    public Interaction<AnibndImportViewModel, List<string>> ChooseAnimationsFromAnibnd { get; } = new();

    public async Task DuplicateClipGeneratorAsync(ClipGeneratorViewModel scg, int taeId)
    {
        string taeSectionId = taeId.ToString("D3");
        string newName = scg.Name[..1] + taeSectionId + scg.Name[4..];
        string newAnimationName = scg.AnimationName[..1] + taeSectionId + scg.AnimationName[4..];
        if (await DoesGeneratorExist(newAnimationName, false)) return;
        hkbClipGenerator selectedGenerator = scg.DuplicateInternal();
        selectedGenerator.m_name = newName;
        selectedGenerator.m_animationName = newAnimationName;
        AddGeneratorWithHistory(selectedGenerator, scg.Parents.ToList());
    }

    public async Task DuplicateAsync()
    {
        ClipGeneratorDupeViewModel dupeDialog = new();
        bool proceed = await GetClipGeneratorDupeOptions.Handle(dupeDialog);
        if (!proceed) return;
        List<int> taeIds = DupeExtensions.GetTaeIdsFromString(dupeDialog.TaeIds);
        List<ClipGeneratorViewModel> selectedClipGenerators = SelectedClipGenerators.ToList();
        foreach (ClipGeneratorViewModel scg in selectedClipGenerators)
        {
            foreach (int id in taeIds)
                await DuplicateClipGeneratorAsync(scg, id);
        }
    }

    public List<CustomManualSelectorGenerator> GetCMSGsByAnimId(int animId)
    {
        _cmsgsByAnimId.TryGetValue(animId, out List<CustomManualSelectorGenerator>? cmsgs);
        return cmsgs ?? new List<CustomManualSelectorGenerator>(0);
    }

    public async Task<bool> DoesGeneratorExist(string animationName, bool showDialogs = true)
    {
        int animId = int.Parse(animationName.Split("_")[1]);
        List<CustomManualSelectorGenerator> cmsgs = GetCMSGsByAnimId(animId);
        if (cmsgs.Count == 0)
        {
            if (showDialogs)
            {
                await ShowMessageBox.Handle(new MessageBoxOptions("Invalid Animation Name",
                    "No suitable CustomManualSelectorGenerators were found for the given animation name.",
                    MessageBoxOptions.MessageBoxMode.Ok));
            }

            return true;
        }

        foreach (CustomManualSelectorGenerator cmsg in cmsgs)
        {
            if (!cmsg.m_generators.Any(x => x is hkbClipGenerator clipGenerator
                                            && clipGenerator.m_animationName == animationName)) continue;
            if (showDialogs)
            {
                await ShowMessageBox.Handle(
                    new MessageBoxOptions("Invalid Animation Name",
                        "A clip generator with this animation name has already been added to the behavior graph.",
                        MessageBoxOptions.MessageBoxMode.Ok));
            }

            return true;
        }

        return false;
    }

    public async Task NewGeneratorAsync()
    {
        ClipGeneratorOptionsViewModel options = new();
        bool proceed = await GetClipGeneratorOptions.Handle(options);
        if (!proceed) return;
        await NewGeneratorAsync(options.AnimationName);
    }

    public async Task NewGeneratorAsync(string animationName)
    {
        int animId = int.Parse(animationName.Split("_")[1]);
        if (await DoesGeneratorExist(animationName)) return;
        hkbClipGenerator clipGenerator = ClipGeneratorViewModel.GetDefaultClipGenerator(animationName);
        List<CustomManualSelectorGenerator> parents = GetCMSGsByAnimId(animId);

        // get mode from first sibling
        if (parents.Count > 0 && parents[0].m_generators.Count > 0)
        {
            clipGenerator.m_mode = ((hkbClipGenerator)parents[0].m_generators[0]!).m_mode;
        }

        AddGeneratorWithHistory(clipGenerator, parents);
    }

    public void AddGeneratorWithHistory(hkbClipGenerator clipGenerator, List<CustomManualSelectorGenerator> parents)
    {
        short nextAnimationInternalId = _nextAnimationInternalId;
        clipGenerator.m_animationInternalId = nextAnimationInternalId;
        ClipGeneratorViewModel viewModel = new(clipGenerator, new List<CustomManualSelectorGenerator>(), _history);
        _history.Snapshot(Undo, Redo);
        Redo();

        void Redo()
        {
            viewModel.AddToGraph(parents);
            ClipGenerators.AddOrUpdate(viewModel);
            CurrentClipGeneratorInternal = viewModel;
            _nextAnimationInternalId = (short)(nextAnimationInternalId + 1);
        }

        void Undo()
        {
            ClipGenerators.Remove(viewModel);
            viewModel.Delete();
            _nextAnimationInternalId = nextAnimationInternalId;
            CurrentClipGeneratorInternal = null;
        }
    }

    public void DeleteCurrentGenerator()
    {
        if (CurrentClipGenerator is null) return;
        DeleteGeneratorWithHistory(CurrentClipGenerator);
    }

    public void DeleteGeneratorWithHistory(ClipGeneratorViewModel viewModel)
    {
        List<CustomManualSelectorGenerator> parents = new(viewModel.Parents);
        _history.Snapshot(Undo, Redo);
        Redo();

        void Redo()
        {
            ClipGenerators.Remove(viewModel);
            viewModel.Delete();
            CurrentClipGeneratorInternal = null;
        }

        void Undo()
        {
            viewModel.AddToGraph(parents);
            ClipGenerators.AddOrUpdate(viewModel);
            CurrentClipGeneratorInternal = viewModel;
        }
    }

    public async Task ImportFromAnibndAsync(BND4 anibnd)
    {
        List<string> newAnimNames = GetNewAnimationNamesFromAnibnd(anibnd);
        if (newAnimNames.Count == 0)
        {
            await ShowMessageBox.Handle(new MessageBoxOptions("Import canceled",
                "The selected anibnd does not contain any unregistered animations.",
                MessageBoxOptions.MessageBoxMode.Ok));
            return;
        }

        AnibndImportViewModel anibndImportViewModel = new(newAnimNames);

        List<string> animNames = await ChooseAnimationsFromAnibnd.Handle(anibndImportViewModel);
        foreach (string animName in animNames)
        {
            await NewGeneratorAsync(animName);
        }
    }

    public List<string> GetNewAnimationNamesFromAnibnd(BND4 anibnd)
    {
        HashSet<string> existingGenerators = ClipGenerators.Items.Select(x => x.AnimationName.ToString()).ToHashSet();
        HashSet<string> ignoredGenerators = Settings.Current.IgnoredAnimationNames.ToHashSet();
        List<string> newAnimationNames = new();
        foreach (BinderFile taeFile in anibnd.Files.Where(x => x.ID is >= 5000000 and < 6000000))
        {
            int taeId = taeFile.ID - 5000000;
            if (!TAE.IsRead(taeFile.Bytes, out TAE tae)) continue;
            newAnimationNames.AddRange(tae.Animations
                .Where(x => _cmsgsByAnimId.ContainsKey((int)x.ID))
                .Select(x => "a" + taeId.ToString("D3") + "_" + x.ID.ToString("D6"))
                .Where(x => !existingGenerators.Contains(x) && !ignoredGenerators.Contains(x)));
        }

        return newAnimationNames;
    }
}