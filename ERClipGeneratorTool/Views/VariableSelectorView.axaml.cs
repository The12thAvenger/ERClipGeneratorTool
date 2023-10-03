using Avalonia;
using Avalonia.ReactiveUI;
using ERClipGeneratorTool.ViewModels;
using ReactiveUI;
using System;

namespace ERClipGeneratorTool.Views;

public partial class VariableSelectorView : ReactiveWindow<VariableSelectorViewModel>
{
    public VariableSelectorView()
    {
        InitializeComponent();
        this.WhenActivated(d =>
        {
            this.Closing += (s, e) =>
            {
                if (e.IsProgrammatic) return;
                e.Cancel = true;
                Close(-1);
            };
            d(ViewModel!.ConfirmCommand.Subscribe(x => Close(x)));
            d(ViewModel!.CancelCommand.Subscribe(x => Close(x)));
        });
#if DEBUG
        this.AttachDevTools();
#endif
    }
}