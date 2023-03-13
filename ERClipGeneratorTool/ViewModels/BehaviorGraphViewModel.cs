﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using DynamicData;
using DynamicData.Binding;
using ERClipGeneratorTool.ViewModels.Interactions;
using HKLib.hk2018;
using ReactiveHistory;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ERClipGeneratorTool.ViewModels;

public class BehaviorGraphViewModel : ViewModelBase
{
    private readonly Dictionary<int, List<CustomManualSelectorGenerator>> _cmsgsByAnimId;
    private readonly IHistory _history;
    private ClipGeneratorViewModel? _currentClipGenerator;
    private short _nextAnimationInternalId;

    public BehaviorGraphViewModel(hkbBehaviorGraph behaviorGraph, List<IHavokObject> objects, IHistory history)
    {
        _history = history;
        BehaviorGraph = behaviorGraph;
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

        ClipGenerators = new SourceCache<ClipGeneratorViewModel, short>(x => x.AnimationInternalId);
        ClipGenerators.AddOrUpdate(clipParents.Select(x => new ClipGeneratorViewModel(x.Key, x.Value, _history)));


        _nextAnimationInternalId = ClipGenerators.Keys.Max();
        _nextAnimationInternalId++;

        _cmsgsByAnimId = cmsgs.GroupBy(x => x.m_animId).ToDictionary(x => x.Key, x => x.ToList());

        IObservable<Func<ClipGeneratorViewModel, bool>> generatorsFilter = this.WhenAnyValue(x => x.Filter)
            .Throttle(TimeSpan.FromMilliseconds(100))
            .Select(x => (Func<ClipGeneratorViewModel, bool>)(y => y.AnimationName.StartsWith(x)));

        FilteredGenerators = new ObservableCollectionExtended<ClipGeneratorViewModel>();
        ClipGenerators.Connect()
            .Filter(generatorsFilter)
            .SortBy(x => x.AnimationName, sortOptimisations: SortOptimisations.IgnoreEvaluates)
            .Bind(FilteredGenerators)
            .Subscribe();

        // this.WhenAnyValue(x => x.CurrentClipGenerator)
        //     .ObserveWithHistory(value => CurrentClipGenerator = value, null, _history);

        NewGeneratorCommand = ReactiveCommand.CreateFromTask(NewGeneratorAsync);

        IObservable<bool> isGeneratorSelected =
            this.WhenAnyValue(x => x.CurrentClipGenerator).Select(x => x is not null);
        DeleteCommand = ReactiveCommand.Create(DeleteCurrentGenerator, isGeneratorSelected);
    }

    [Reactive] public string Filter { get; set; } = "";

    public string? Path { get; set; }
    public hkbBehaviorGraph BehaviorGraph { get; }
    public SourceCache<ClipGeneratorViewModel, short> ClipGenerators { get; }

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

    public async Task NewGeneratorAsync()
    {
        ClipGeneratorOptionsViewModel options = new();
        bool proceed = await GetClipGeneratorOptions.Handle(options);
        if (!proceed) return;

        int animId = int.Parse(options.AnimationName.Split("_")[1]);
        if (!_cmsgsByAnimId.TryGetValue(animId, out List<CustomManualSelectorGenerator>? cmsgs))
        {
            await ShowMessageBox.Handle(new MessageBoxOptions("Invalid Animation Name",
                "No suitable CustomManualSelectorGenerators were found for the given animation name.",
                MessageBoxOptions.MessageBoxMode.Ok));
            return;
        }

        foreach (CustomManualSelectorGenerator cmsg in cmsgs)
        {
            if (cmsg.m_generators.Any(x => x is hkbClipGenerator clipGenerator
                                           && clipGenerator.m_animationName == options.AnimationName))
            {
                await ShowMessageBox.Handle(
                    new MessageBoxOptions("Invalid Animation Name",
                        "A clip generator with this animation name has already been added to the behavior graph.",
                        MessageBoxOptions.MessageBoxMode.Ok));
                return;
            }
        }

        hkbClipGenerator clipGenerator = new()
        {
            m_propertyBag = new hkPropertyBag(),
            m_variableBindingSet = null,
            m_userData = 0,
            m_name = options.AnimationName,
            m_animationName = options.AnimationName,
            m_triggers = null,
            m_userPartitionMask = 0,
            m_cropStartAmountLocalTime = 0,
            m_cropEndAmountLocalTime = 0,
            m_startTime = 0,
            m_playbackSpeed = 1,
            m_enforcedDuration = 0,
            m_userControlledTimeFraction = 0,
            m_mode = hkbClipGenerator.PlaybackMode.MODE_SINGLE_PLAY,
            m_flags = 0,
            m_animationInternalId = _nextAnimationInternalId
        };
        AddGeneratorWithHistory(clipGenerator, cmsgs);
    }

    public void AddGeneratorWithHistory(hkbClipGenerator clipGenerator, List<CustomManualSelectorGenerator> parents)
    {
        ClipGeneratorViewModel viewModel = new(clipGenerator, new List<CustomManualSelectorGenerator>(), _history);
        short nextAnimationInternalId = _nextAnimationInternalId;
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
}