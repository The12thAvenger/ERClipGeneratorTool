using Avalonia;
using Avalonia.ReactiveUI;
using ERClipGeneratorTool.ViewModels;
using ReactiveUI;
using System;

namespace ERClipGeneratorTool.Views;

public partial class AnibndImportView : ReactiveWindow<AnibndImportViewModel>
{
    public AnibndImportView()
    {
        InitializeComponent();

        this.WhenActivated(d =>
        {
            d(ViewModel!.ConfirmCommand.Subscribe(Close));
            d(ViewModel!.CancelCommand.Subscribe(Close));
        });
#if DEBUG
        this.AttachDevTools();
#endif
    }
}