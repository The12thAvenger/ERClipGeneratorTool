using System;
using System.ComponentModel.DataAnnotations;
using System.Reactive;
using System.Reactive.Linq;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ERClipGeneratorTool.ViewModels;

public class ClipGeneratorDupeViewModel : ViewModelBase
{
    public ClipGeneratorDupeViewModel()
    {
        IObservable<bool> isValid = this.WhenAnyValue(x => x.TaeIds).Select(x =>
            ClipGeneratorViewModel.ValidateOffsets(x) == ValidationResult.Success);
        ConfirmCommand = ReactiveCommand.Create(() => true, isValid);
        CancelCommand = ReactiveCommand.Create(() => false);
    }

    [Reactive]
    [CustomValidation(typeof(ClipGeneratorViewModel), nameof(ClipGeneratorViewModel.ValidateOffsets))]
    public string TaeIds { get; set; } = "0";

    public ReactiveCommand<Unit, bool> ConfirmCommand { get; }

    public ReactiveCommand<Unit, bool> CancelCommand { get; }
}