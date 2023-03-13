using System.ComponentModel.DataAnnotations;
using Avalonia.ReactiveUI;
using ERClipGeneratorTool.ViewModels;
using ReactiveUI;

namespace ERClipGeneratorTool.Views;

public partial class ClipGeneratorView : ReactiveUserControl<ClipGeneratorViewModel>
{
    public ClipGeneratorView()
    {
        InitializeComponent();

        this.WhenActivated(_ =>
        {
            ValidationContext validationContext = new(ViewModel!)
            {
                MemberName = nameof(ClipGeneratorViewModel.AnimationName)
            };
            AnimationName.ValidationContext = validationContext;
        });
    }
}