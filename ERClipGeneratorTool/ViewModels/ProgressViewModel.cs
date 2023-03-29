using ReactiveUI.Fody.Helpers;

namespace ERClipGeneratorTool.ViewModels;

public class ProgressViewModel : ViewModelBase
{
    [Reactive] public string Status { get; set; } = "";

    [Reactive] public bool IsActive { get; set; }
}