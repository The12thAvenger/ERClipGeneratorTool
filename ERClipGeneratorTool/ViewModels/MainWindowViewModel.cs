using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using ERClipGeneratorTool.ViewModels.Interactions;
using HKLib.hk2018;
using HKLib.Serialization.hk2018.Binary;
using ReactiveHistory;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SoulsFormats;

namespace ERClipGeneratorTool.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    public MainWindowViewModel()
    {
        History = new StackHistory();

        this.WhenAnyValue(x => x.History).Subscribe(x =>
        {
            UndoCommand = ReactiveCommand.Create(x.Undo, x.CanUndo);
            RedoCommand = ReactiveCommand.Create(x.Redo, x.CanRedo);
        });

        OpenFileCommand = ReactiveCommand.CreateFromTask(OpenFileAsync);
        SaveFileCommand = ReactiveCommand.Create(SaveFile);
        SaveFileAsCommand = ReactiveCommand.CreateFromTask(SaveFileAsAsync);
    }

    [Reactive] private IHistory History { get; set; }

    [Reactive] public BehaviorGraphViewModel? BehaviorGraph { get; private set; }

    [Reactive] public ObservableCollection<ClipGeneratorViewModel> ClipGenerators { get; set; } = new();

    public ReactiveCommand<Unit, Unit> OpenFileCommand { get; }

    public ReactiveCommand<Unit, Unit> SaveFileCommand { get; }

    public ReactiveCommand<Unit, Unit> SaveFileAsCommand { get; }

    [Reactive] public ReactiveCommand<Unit, bool>? UndoCommand { get; set; }

    [Reactive] public ReactiveCommand<Unit, bool>? RedoCommand { get; set; }

    public Interaction<FilePathOptions, string?> GetFilePath { get; } = new();

    private async Task OpenFileAsync()
    {
        List<FilePathOptions.Filter> filters = new()
        {
            // new FilePathOptions.Filter("Elden Ring Behbnds", new List<string> { "behbnd*" }),
            new FilePathOptions.Filter("Havok Binary Packfiles", new List<string> { "hkx" }),
            new FilePathOptions.Filter("All Files", new List<string> { "*" })
        };

        FilePathOptions options = new("Open Behavior File", FilePathOptions.FilePathMode.Open, filters);

        HavokBinarySerializer serializer = new();
        while (true)
        {
            string? path = await GetFilePath.Handle(options);
            if (path is null) return;

            if (path.EndsWith(".behbnd.dcx") || path.EndsWith(".behbnd"))
            {
                if (!BND4.IsRead(path, out BND4 bnd))
                {
                    await ShowMessageBox.Handle(new MessageBoxOptions("Error Loading File",
                        "The selected file is not a valid behbnd.",
                        MessageBoxOptions.MessageBoxMode.Ok));
                    continue;
                }
            }

            List<IHavokObject> objects;
            try
            {
                objects = serializer.ReadAllObjects(path).ToList();
            }
            catch (Exception)
            {
                await ShowMessageBox.Handle(new MessageBoxOptions("Error Loading File",
                    "The selected file is not a valid Havok 2018 binary tagfile.",
                    MessageBoxOptions.MessageBoxMode.Ok));
                continue;
            }

            if (objects.Count == 0 || objects[0] is not hkRootLevelContainer rootLevelContainer)
            {
                await ShowMessageBox.Handle(new MessageBoxOptions("Error Loading File",
                    "The selected file does not contain an hkRootLevelContainer.",
                    MessageBoxOptions.MessageBoxMode.Ok));
                continue;
            }

            if (rootLevelContainer.m_namedVariants.Count == 0 ||
                rootLevelContainer.m_namedVariants[0].m_variant is not hkbBehaviorGraph)
            {
                await ShowMessageBox.Handle(new MessageBoxOptions("Error Loading File",
                    "The selected file does not contain an hkbBehaviorGraph.",
                    MessageBoxOptions.MessageBoxMode.Ok));
                continue;
            }

            if (!objects.Any(x => x is hkbClipGenerator))
            {
                await ShowMessageBox.Handle(new MessageBoxOptions("Error Loading File",
                    "The selected file does not contain any hkbClipGenerators.",
                    MessageBoxOptions.MessageBoxMode.Ok));
                continue;
            }

            History = new StackHistory();
            BehaviorGraph = new BehaviorGraphViewModel(rootLevelContainer, objects, History)
            {
                Path = path,
            };
            break;
        }
    }

    private List<IHavokObject> GetFileFromBnd(BND4 bnd)
    {
        throw new NotImplementedException();
    }

    private void SaveFile()
    {
        HavokBinarySerializer serializer = new();
        serializer.Write(BehaviorGraph!.RootLevelContainer, BehaviorGraph.Path!);
    }

    private async Task SaveFileAsAsync()
    {
        List<FilePathOptions.Filter> filters = new()
        {
            new FilePathOptions.Filter("Havok Binary Packfiles", new List<string> { "hkx" }),
            new FilePathOptions.Filter("All Files", new List<string> { "*" })
        };

        FilePathOptions options = new("Save File As...", FilePathOptions.FilePathMode.Save, filters);

        string? path = await GetFilePath.Handle(options);
        if (path is null) return;

        BehaviorGraph!.Path = path;
        SaveFile();
    }
}