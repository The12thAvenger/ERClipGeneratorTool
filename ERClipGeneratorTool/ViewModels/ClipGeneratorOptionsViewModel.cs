using System;
using System.ComponentModel.DataAnnotations;
using System.Reactive;
using Reactive.Bindings.TinyLinq;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ERClipGeneratorTool.ViewModels;

public class ClipGeneratorOptionsViewModel : ViewModelBase
{
    public ClipGeneratorOptionsViewModel()
    {
        IObservable<bool> isValid = this.WhenAnyValue(x => x.AnimationName).Select(x =>
            ClipGeneratorViewModel.ValidateAnimationName(x) == ValidationResult.Success);

        ConfirmCommand = ReactiveCommand.Create(() => true, isValid);
        CancelCommand = ReactiveCommand.Create(() => false);
    }

    [Reactive]
    [CustomValidation(typeof(ClipGeneratorViewModel), nameof(ClipGeneratorViewModel.ValidateAnimationName))]
    public string AnimationName { get; set; } = "a000_000000";


    public ReactiveCommand<Unit, bool> ConfirmCommand { get; }

    public ReactiveCommand<Unit, bool> CancelCommand { get; }
}