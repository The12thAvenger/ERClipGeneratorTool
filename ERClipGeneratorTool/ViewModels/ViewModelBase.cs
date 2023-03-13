using ERClipGeneratorTool.ViewModels.Interactions;
using ReactiveUI;

namespace ERClipGeneratorTool.ViewModels
{
    public class ViewModelBase : ReactiveObject
    {
        public Interaction<MessageBoxOptions, MessageBoxOptions.MessageBoxResult> ShowMessageBox { get; } = new();
    }
}