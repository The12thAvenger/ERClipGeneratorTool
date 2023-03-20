using Avalonia;
using Avalonia.ReactiveUI;
using ERClipGeneratorTool.ViewModels;
using ReactiveUI;
using System;

namespace ERClipGeneratorTool.Views;

public partial class ClipGeneratorDupeView : ReactiveWindow<ClipGeneratorDupeViewModel>
{
    public ClipGeneratorDupeView()
    {
        InitializeComponent();
        this.WhenActivated(d =>
        {
            d(ViewModel!.ConfirmCommand.Subscribe(x => Close(x)));
            d(ViewModel!.CancelCommand.Subscribe(x => Close(x)));
        });
#if DEBUG
        this.AttachDevTools();
#endif
    }
}