using Avalonia;
using Avalonia.ReactiveUI;
using ERClipGeneratorTool.ViewModels;
using ReactiveUI;
using System;
using System.Reactive.Disposables;

namespace ERClipGeneratorTool.Views;

public partial class ClipGeneratorDupeView : ReactiveWindow<ClipGeneratorDupeViewModel>
{
    public ClipGeneratorDupeView()
    {
        InitializeComponent();
        this.WhenActivated(d =>
        {
            ViewModel!.ConfirmCommand.Subscribe(x => Close(x)).DisposeWith(d);
            ViewModel!.CancelCommand.Subscribe(x => Close(x)).DisposeWith(d);
        });
#if DEBUG
        this.AttachDevTools();
#endif
    }
}