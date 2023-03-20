using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ERClipGeneratorTool.ViewModels;

public class AnibndImportViewModel : ViewModelBase
{
    public AnibndImportViewModel(IEnumerable<string> animationNames)
    {
        Animations = new ObservableCollection<AnimationEntryViewModel>(
            animationNames.Select(x => new AnimationEntryViewModel(x)));

        ConfirmCommand =
            ReactiveCommand.Create(() => Animations.Where(x => x.Import).Select(x => x.AnimationName).ToList());
        CancelCommand = ReactiveCommand.Create(() => new List<string>());
    }

    public ObservableCollection<AnimationEntryViewModel> Animations { get; }

    public ReactiveCommand<Unit, List<string>> ConfirmCommand { get; }

    public ReactiveCommand<Unit, List<string>> CancelCommand { get; }
}

public class AnimationEntryViewModel : ViewModelBase
{
    public AnimationEntryViewModel(string animationName)
    {
        AnimationName = animationName;
    }

    public string AnimationName { get; }
    [Reactive] public bool Import { get; set; } = true;
}