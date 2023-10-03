using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.ReactiveUI;
using ERClipGeneratorTool.ViewModels;
using ERClipGeneratorTool.ViewModels.Interactions;
using MsBox.Avalonia;
using MsBox.Avalonia.Base;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using ReactiveUI;

namespace ERClipGeneratorTool.Views;

public partial class MainWindow : ReactiveWindow<MainWindowViewModel>
{
    public MainWindow()
    {
        InitializeComponent();

        this.WhenActivated(d =>
        {
            ViewModel!.GetFileSource.RegisterHandler(GetFilePathAsync).DisposeWith(d);
            GlobalInteractions.ShowMessageBox.RegisterHandler(ShowMessageBoxAsync).DisposeWith(d);
            ViewModel!.GetBndFileName.RegisterHandler(GetBndFileNameAsync).DisposeWith(d);
        });
    }

    private async Task ShowMessageBoxAsync(
        IInteractionContext<MessageBoxOptions, MessageBoxOptions.MessageBoxResult> interaction)
    {
        MessageBoxOptions options = interaction.Input;
        interaction.SetOutput(await ShowMessageBoxAsync(options));
    }

    private async Task<MessageBoxOptions.MessageBoxResult> ShowMessageBoxAsync(MessageBoxOptions options)
    {
        ButtonEnum mode = (ButtonEnum)options.Mode;
        IMsBox<ButtonResult> messageBox = MessageBoxManager.GetMessageBoxStandard(new MessageBoxStandardParams
        {
            ButtonDefinitions = mode,
            CanResize = false,
            ContentTitle = options.Header,
            ContentMessage = options.Message,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            WindowIcon = Icon
        });
        ButtonResult result = await messageBox.ShowWindowDialogAsync(this);
        return (MessageBoxOptions.MessageBoxResult)result;
    }

    private async Task GetFilePathAsync(IInteractionContext<FilePathOptions, FileSource?> interaction)
    {
        string? path;
        switch (interaction.Input.Mode)
        {
            case FilePathOptions.FilePathMode.Open:
            {
                FilePickerOpenOptions openOptions = new()
                {
                    Title = interaction.Input.Prompt,
                    FileTypeFilter = interaction.Input.Filters
                        .Select(x => new FilePickerFileType(x.Name) { Patterns = x.Extensions }).ToList(),
                    AllowMultiple = false
                };

                IReadOnlyList<IStorageFile> openFiles = await StorageProvider.OpenFilePickerAsync(openOptions);
                path = openFiles.Count == 0 ? null : openFiles[0].Path.LocalPath;
                break;
            }
            case FilePathOptions.FilePathMode.Save:
                FilePickerSaveOptions saveOptions = new()
                {
                    Title = interaction.Input.Prompt,
                    FileTypeChoices = interaction.Input.Filters
                        .Select(x => new FilePickerFileType(x.Name) { Patterns = x.Extensions }).ToList()
                };

                IStorageFile? saveFile = await StorageProvider.SaveFilePickerAsync(saveOptions);
                path = saveFile?.Path.LocalPath;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(interaction));
        }

        interaction.SetOutput(path is null ? null : new FileSource(path));
    }

    private async Task GetBndFileNameAsync(IInteractionContext<BndOpenViewModel, string?> interaction)
    {
        BndOpenView view = new()
        {
            DataContext = interaction.Input
        };

        interaction.SetOutput(await view.ShowDialog<string?>(this));
    }
}