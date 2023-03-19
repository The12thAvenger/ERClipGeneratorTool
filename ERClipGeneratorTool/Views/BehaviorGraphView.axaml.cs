using System.Reactive.Disposables;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using Avalonia.VisualTree;
using ERClipGeneratorTool.ViewModels;
using ERClipGeneratorTool.ViewModels.Interactions;
using MessageBox.Avalonia;
using MessageBox.Avalonia.BaseWindows.Base;
using MessageBox.Avalonia.Enums;
using ReactiveUI;

namespace ERClipGeneratorTool.Views;

public partial class BehaviorGraphView : ReactiveUserControl<BehaviorGraphViewModel>
{
    public BehaviorGraphView()
    {
        InitializeComponent();

        this.WhenActivated(d =>
        {
            ViewModel!.ShowMessageBox.RegisterHandler(ShowMessageBoxAsync).DisposeWith(d);
            ViewModel!.GetClipGeneratorOptions.RegisterHandler(GetClipGeneratorOptionsAsync).DisposeWith(d);
            ViewModel!.GetClipGeneratorDupeOptions.RegisterHandler(GetClipGeneratorDupeOptionsAsync).DisposeWith(d);
        });
    }

    private async Task GetClipGeneratorOptionsAsync(InteractionContext<ClipGeneratorOptionsViewModel, bool> interaction)
    {
        ClipGeneratorOptionsView view = new()
        {
            DataContext = interaction.Input
        };

        interaction.SetOutput(await view.ShowDialog<bool>(this.FindAncestorOfType<Window>()!));
    }

    private async Task GetClipGeneratorDupeOptionsAsync(InteractionContext<ClipGeneratorDupeViewModel, bool> interaction)
    {
        ClipGeneratorDupeView view = new()
        {
            DataContext = interaction.Input
        };

        interaction.SetOutput(await view.ShowDialog<bool>(this.FindAncestorOfType<Window>()!));
    }

    private async Task ShowMessageBoxAsync(
        InteractionContext<MessageBoxOptions, MessageBoxOptions.MessageBoxResult> interaction)
    {
        MessageBoxOptions options = interaction.Input;
        ButtonEnum mode = (ButtonEnum)options.Mode;
        IMsBoxWindow<ButtonResult> messageBox =
            MessageBoxManager.GetMessageBoxStandardWindow(options.Header, options.Message, mode);

        ButtonResult result = await messageBox.Show(this.FindAncestorOfType<Window>());
        interaction.SetOutput((MessageBoxOptions.MessageBoxResult)result);
    }
}