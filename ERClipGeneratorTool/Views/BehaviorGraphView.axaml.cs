using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using Avalonia.VisualTree;
using ERClipGeneratorTool.ViewModels;
using ReactiveUI;

namespace ERClipGeneratorTool.Views;

public partial class BehaviorGraphView : ReactiveUserControl<BehaviorGraphViewModel>
{
    public BehaviorGraphView()
    {
        InitializeComponent();

        this.WhenActivated(d =>
        {
            ViewModel!.GetClipGeneratorOptions.RegisterHandler(GetClipGeneratorOptionsAsync).DisposeWith(d);
            ViewModel!.GetClipGeneratorDupeOptions.RegisterHandler(GetClipGeneratorDupeOptionsAsync).DisposeWith(d);
            ViewModel!.ChooseAnimationsFromAnibnd.RegisterHandler(ChooseAnimationsFromAnibndAsync).DisposeWith(d);
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

    private async Task GetClipGeneratorDupeOptionsAsync(
        InteractionContext<ClipGeneratorDupeViewModel, bool> interaction)
    {
        ClipGeneratorDupeView view = new()
        {
            DataContext = interaction.Input
        };

        interaction.SetOutput(await view.ShowDialog<bool>(this.FindAncestorOfType<Window>()!));
    }

    private async Task ChooseAnimationsFromAnibndAsync(
        InteractionContext<AnibndImportViewModel, List<string>> interaction)
    {
        AnibndImportView view = new()
        {
            DataContext = interaction.Input
        };

        interaction.SetOutput(await view.ShowDialog<List<string>?>(this.FindAncestorOfType<Window>()!) ??
                              new List<string>());
    }
}