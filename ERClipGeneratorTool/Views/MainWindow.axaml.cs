using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using Avalonia.Controls;
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
            ViewModel!.GetFilePath.RegisterHandler(GetFilePathAsync).DisposeWith(d);
            ViewModel!.ShowMessageBox.RegisterHandler(ShowMessageBoxAsync).DisposeWith(d);
        });
    }

    private async Task ShowMessageBoxAsync(
        InteractionContext<MessageBoxOptions, MessageBoxOptions.MessageBoxResult> interaction)
    {
        MessageBoxOptions options = interaction.Input;
        ButtonEnum mode = (ButtonEnum)options.Mode;
        IMsBoxWindow<ButtonResult> messageBox =
            MessageBoxManager.GetMessageBoxStandardWindow(new MessageBoxStandardParams()
            {
                ButtonDefinitions = mode,
                CanResize = false,
                ContentTitle = options.Header,
                ContentMessage = options.Message,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                SystemDecorations = SystemDecorations.BorderOnly,
            });

        ButtonResult result = await messageBox.Show(this);
        interaction.SetOutput((MessageBoxOptions.MessageBoxResult)result);
    }

    private async Task GetFilePathAsync(InteractionContext<FilePathOptions, string?> interaction)
    {
        string? path;
        switch (interaction.Input.Mode)
        {
            case FilePathOptions.FilePathMode.Open:
            {
                OpenFileDialog openFileDialog = new()
                {
                    Title = interaction.Input.Prompt,
                    Filters = interaction.Input.Filters
                        .Select(x => new FileDialogFilter { Name = x.Name, Extensions = x.Extensions }).ToList(),
                    AllowMultiple = false
                };

                string[]? paths = await openFileDialog.ShowAsync(this);
                if (paths is null || paths.Length == 0)
                {
                    path = null;
                }
                else
                {
                    path = paths[0];
                }

                break;
            }
            case FilePathOptions.FilePathMode.Save:
                SaveFileDialog saveFileDialog = new()
                {
                    Title = interaction.Input.Prompt,
                    Filters = interaction.Input.Filters
                        .Select(x => new FileDialogFilter { Name = x.Name, Extensions = x.Extensions }).ToList()
                };

                path = await saveFileDialog.ShowAsync(this);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(interaction));
        }

        interaction.SetOutput(path);
    }
}