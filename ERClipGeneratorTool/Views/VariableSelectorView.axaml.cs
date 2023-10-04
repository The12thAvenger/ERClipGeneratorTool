using Avalonia;
using Avalonia.ReactiveUI;
using ERClipGeneratorTool.ViewModels;
using ReactiveUI;
using System;
using System.Reactive.Disposables;

namespace ERClipGeneratorTool.Views;

public partial class VariableSelectorView : ReactiveWindow<VariableSelectorViewModel>
{
    public VariableSelectorView()
    {
        InitializeComponent();
        this.WhenActivated(d =>
        {
            Closing += (_, e) =>
            {
                if (e.IsProgrammatic) return;
                e.Cancel = true;
                Close(-1);
            };
            ViewModel!.ConfirmCommand.Subscribe(x => Close(x)).DisposeWith(d);
            ViewModel!.CancelCommand.Subscribe(x => Close(x)).DisposeWith(d);
        });
#if DEBUG
        this.AttachDevTools();
#endif
    }
}