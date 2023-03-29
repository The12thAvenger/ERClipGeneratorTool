using ReactiveUI;

namespace ERClipGeneratorTool.ViewModels.Interactions;

public static class GlobalInteractions
{
    public static Interaction<MessageBoxOptions, MessageBoxOptions.MessageBoxResult> ShowMessageBox { get; } = new();
}