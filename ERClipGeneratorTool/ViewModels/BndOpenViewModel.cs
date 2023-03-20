using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using Reactive.Bindings.TinyLinq;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SoulsFormats;

namespace ERClipGeneratorTool.ViewModels;

public class BndOpenViewModel : ViewModelBase
{
    public BndOpenViewModel(IBinder bnd)
    {
        Files = new ObservableCollection<BndFileViewModel>(bnd.Files.Select(x => new BndFileViewModel(x.Name)));

        IObservable<bool> isValid = this.WhenAnyValue(x => x.SelectedFile).Select(x => x is not null);

        ConfirmCommand = ReactiveCommand.Create(() => (string?)SelectedFile!.Name, isValid);
        CancelCommand = ReactiveCommand.Create(() => (string?)null);
    }

    public ObservableCollection<BndFileViewModel> Files { get; }
    [Reactive] public BndFileViewModel? SelectedFile { get; set; }
    public ReactiveCommand<Unit, string?> ConfirmCommand { get; }
    public ReactiveCommand<Unit, string?> CancelCommand { get; }
}

public class BndFileViewModel
{
    public BndFileViewModel(string name)
    {
        Name = name;
    }

    public string Name { get; }
}