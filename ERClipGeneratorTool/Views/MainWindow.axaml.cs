using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.ReactiveUI;
using ERClipGeneratorTool.ViewModels;
using ERClipGeneratorTool.ViewModels.Interactions;
using MessageBox.Avalonia;
using MessageBox.Avalonia.BaseWindows.Base;
using MessageBox.Avalonia.DTO;
using MessageBox.Avalonia.Enums;
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
            ViewModel!.ShowMessageBox.RegisterHandler(ShowMessageBoxAsync).DisposeWith(d);
            ViewModel!.GetBndFileName.RegisterHandler(GetBndFileNameAsync).DisposeWith(d);
        });
    }

    private async Task ShowMessageBoxAsync(
        InteractionContext<MessageBoxOptions, MessageBoxOptions.MessageBoxResult> interaction)
    {
        MessageBoxOptions options = interaction.Input;
        interaction.SetOutput(await ShowMessageBoxAsync(options));
    }

    private async Task<MessageBoxOptions.MessageBoxResult> ShowMessageBoxAsync(MessageBoxOptions options)
    {
        ButtonEnum mode = (ButtonEnum)options.Mode;
        IMsBoxWindow<ButtonResult> messageBox =
            MessageBoxManager.GetMessageBoxStandardWindow(new MessageBoxStandardParams
            {
                ButtonDefinitions = mode,
                CanResize = false,
                ContentTitle = options.Header,
                ContentMessage = options.Message,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                WindowIcon = Icon
            });
        ButtonResult result = await messageBox.Show(this);
        return (MessageBoxOptions.MessageBoxResult)result;
    }

    private async Task GetFilePathAsync(InteractionContext<FilePathOptions, FileSource?> interaction)
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

    private async Task GetBndFileNameAsync(InteractionContext<BndOpenViewModel, string?> interaction)
    {
        BndOpenView view = new()
        {
            DataContext = interaction.Input
        };

        interaction.SetOutput(await view.ShowDialog<string?>(this));
    }
}