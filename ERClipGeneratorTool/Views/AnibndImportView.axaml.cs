using Avalonia;
using Avalonia.ReactiveUI;
using ERClipGeneratorTool.ViewModels;
using ReactiveUI;
using System;
using System.Reactive.Disposables;

namespace ERClipGeneratorTool.Views;

public partial class AnibndImportView : ReactiveWindow<AnibndImportViewModel>
{
    public AnibndImportView()
    {
        InitializeComponent();

        this.WhenActivated(d =>
        {
            ViewModel!.ConfirmCommand.Subscribe(Close).DisposeWith(d);
            ViewModel!.CancelCommand.Subscribe(Close).DisposeWith(d);
        });
#if DEBUG
        this.AttachDevTools();
#endif
    }
}