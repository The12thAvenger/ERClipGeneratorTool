using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using DynamicData.Binding;
using ERClipGeneratorTool.Util;
using ERClipGeneratorTool.ViewModels.Interactions;
using HKLib.hk2018;
using HKLib.Serialization.hk2018;
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
        OpenRecentFileCommand = ReactiveCommand.CreateFromTask<FileSource>(OpenRecentFileAsync);
        ImportFromAnibndCommand = ReactiveCommand.CreateFromTask(ImportFromAnibndAsync);
        SaveFileCommand = ReactiveCommand.Create(SaveFile);
        SaveFileAsCommand = ReactiveCommand.CreateFromTask(SaveFileAsAsync);

        RecentFiles.ToObservableChangeSet().IsEmpty().Select(x => !x).ToPropertyEx(this, x => x.HasRecentFiles);
    }


    public ObservableCollection<FileSource> RecentFiles => Settings.Current.RecentFiles;

    [ObservableAsProperty] public bool HasRecentFiles { get; }

    [Reactive] private IHistory History { get; set; }

    [Reactive] public BehaviorGraphViewModel? BehaviorGraph { get; private set; }

    public ReactiveCommand<Unit, Unit> OpenFileCommand { get; }

    public ReactiveCommand<FileSource, Unit> OpenRecentFileCommand { get; }
    public ReactiveCommand<Unit, Unit> ImportFromAnibndCommand { get; }

    public ReactiveCommand<Unit, Unit> SaveFileCommand { get; }

    public ReactiveCommand<Unit, Unit> SaveFileAsCommand { get; }

    [Reactive] public ReactiveCommand<Unit, bool>? UndoCommand { get; set; }

    [Reactive] public ReactiveCommand<Unit, bool>? RedoCommand { get; set; }

    public Interaction<FilePathOptions, FileSource?> GetFileSource { get; } = new();
    public Interaction<BndOpenViewModel, string?> GetBndFileName { get; } = new();

    private async Task OpenFileAsync()
    {
        List<FilePathOptions.Filter> filters = new()
        {
            new FilePathOptions.Filter("Elden Ring Behbnds", new List<string> { "*.behbnd*" }),
            new FilePathOptions.Filter("Havok Binary Packfiles", new List<string> { "*.hkx" }),
            new FilePathOptions.Filter("All Files", new List<string> { "*.*" })
        };

        FilePathOptions options = new("Open Behavior File", FilePathOptions.FilePathMode.Open, filters);

        HavokBinarySerializer serializer = new();
        while (true)
        {
            (bool abort, FileSource? source) = await GetBehaviorFileSource(options);
            if (abort) return;

            if (source is not null && await OpenFileAsync(serializer, source)) break;
        }
    }

    private async Task OpenRecentFileAsync(FileSource fileSource)
    {
        if (!File.Exists(fileSource.FilePath))
        {
            await ShowMessageBox.Handle(new MessageBoxOptions("Error Loading File",
                "The selected file no longer exists.",
                MessageBoxOptions.MessageBoxMode.Ok));
        }

        if (await OpenFileAsync(new HavokBinarySerializer(), fileSource)) return;

        RemoveRecentFile(fileSource);
    }

    private async Task<(bool Abort, FileSource? Source)> GetBehaviorFileSource(FilePathOptions options)
    {
        FileSource? source = await GetFileSource.Handle(options);
        if (source is null) return (true, null);

        if (source.FilePath.EndsWith(".behbnd.dcx") || source.FilePath.EndsWith(".behbnd"))
        {
            if (!File.Exists(source.FilePath))
            {
                await ShowMessageBox.Handle(new MessageBoxOptions("Error Loading Binder",
                    "The specified binder file does not exist. Behavior files can only be saved to existing behbnds.",
                    MessageBoxOptions.MessageBoxMode.Ok));
                return (false, null);
            }

            if (!BND4.IsRead(source.FilePath, out BND4 bnd))
            {
                await ShowMessageBox.Handle(new MessageBoxOptions("Error Loading Binder",
                    "The selected file is not a valid binder file.",
                    MessageBoxOptions.MessageBoxMode.Ok));
                return (false, null);
            }

            string? bndFileName = await GetBndFileName.Handle(new BndOpenViewModel(bnd));
            if (bndFileName is null) return (false, null);

            source.BndFileName = bndFileName;
        }

        return (false, source);
    }

    private async Task<bool> OpenFileAsync(HavokSerializer serializer, FileSource fileSource)
    {
        List<IHavokObject> objects;
        try
        {
            objects = serializer.ReadAllObjects(fileSource.GetReadStream()).ToList();
        }
        catch (InvalidDataException)
        {
            await ShowMessageBox.Handle(new MessageBoxOptions("Error Loading File",
                "The selected file is not a valid Havok 2018 binary tagfile.",
                MessageBoxOptions.MessageBoxMode.Ok));
            return false;
        }
        catch (Exception)
        {
            await ShowMessageBox.Handle(new MessageBoxOptions("Error Loading File",
                "Failed to read the selected file.",
                MessageBoxOptions.MessageBoxMode.Ok));
            return false;
        }

        if (objects.Count == 0 || objects[0] is not hkRootLevelContainer rootLevelContainer)
        {
            await ShowMessageBox.Handle(new MessageBoxOptions("Error Loading File",
                "The selected file does not contain an hkRootLevelContainer.",
                MessageBoxOptions.MessageBoxMode.Ok));
            return false;
        }

        if (rootLevelContainer.m_namedVariants.Count == 0 ||
            rootLevelContainer.m_namedVariants[0].m_variant is not hkbBehaviorGraph)
        {
            await ShowMessageBox.Handle(new MessageBoxOptions("Error Loading File",
                "The selected file does not contain an hkbBehaviorGraph.",
                MessageBoxOptions.MessageBoxMode.Ok));
            return false;
        }

        if (!objects.Any(x => x is hkbClipGenerator))
        {
            await ShowMessageBox.Handle(new MessageBoxOptions("Error Loading File",
                "The selected file does not contain any hkbClipGenerators.",
                MessageBoxOptions.MessageBoxMode.Ok));
            return false;
        }

        AddRecentFile(fileSource);

        History = new StackHistory();
        BehaviorGraph = new BehaviorGraphViewModel(rootLevelContainer, objects, History, fileSource);
        return true;
    }

    private static void AddRecentFile(FileSource fileSource)
    {
        int i = 0;
        while (i < Settings.Current.RecentFiles.Count)
        {
            FileSource recentFile = Settings.Current.RecentFiles[i];
            if (recentFile.DisplayPath == fileSource.DisplayPath)
            {
                Settings.Current.RecentFiles.RemoveAt(i);
                continue;
            }

            i++;
        }

        Settings.Current.RecentFiles.Insert(0, fileSource);
        while (Settings.Current.RecentFiles.Count > 10)
        {
            Settings.Current.RecentFiles.RemoveAt(10);
        }

        Settings.Current.Save();
    }

    private static void RemoveRecentFile(FileSource fileSource)
    {
        Settings.Current.RecentFiles.Remove(fileSource);
        Settings.Current.Save();
    }

    private async Task ImportFromAnibndAsync()
    {
        if (BehaviorGraph is null) return;
        List<FilePathOptions.Filter> filters = new()
        {
            new FilePathOptions.Filter("Elden Ring Anibnds", new List<string> { "*.anibnd*" }),
            new FilePathOptions.Filter("All Files", new List<string> { "*.*" })
        };

        FilePathOptions options = new("Open Anibnd", FilePathOptions.FilePathMode.Open, filters);
        while (true)
        {
            FileSource? source = await GetFileSource.Handle(options);
            if (source is null) return;

            if (!BND4.IsRead(source.FilePath, out BND4 bnd))
            {
                await ShowMessageBox.Handle(new MessageBoxOptions("Error Loading File",
                    "The selected file is not a valid anibnd.",
                    MessageBoxOptions.MessageBoxMode.Ok));
                continue;
            }

            await BehaviorGraph.ImportFromAnibndAsync(bnd);
            break;
        }
    }

    private void SaveFile()
    {
        HavokBinarySerializer serializer = new();
        MemoryStream stream = new();
        serializer.Write(BehaviorGraph!.RootLevelContainer, stream);
        BehaviorGraph.FileSource.Write(stream.ToArray());
    }

    private async Task SaveFileAsAsync()
    {
        List<FilePathOptions.Filter> filters = new()
        {
            new FilePathOptions.Filter("Elden Ring Behbnds", new List<string> { "*.behbnd*" }),
            new FilePathOptions.Filter("Havok Binary Packfiles", new List<string> { "*.hkx" }),
            new FilePathOptions.Filter("All Files", new List<string> { "*.*" })
        };

        FilePathOptions options = new("Save File As...", FilePathOptions.FilePathMode.Save, filters);

        while (true)
        {
            (bool abort, FileSource? source) = await GetBehaviorFileSource(options);
            if (abort) return;
            if (source is null) continue;

            BehaviorGraph!.FileSource = source;
            SaveFile();
            break;
        }
    }
}