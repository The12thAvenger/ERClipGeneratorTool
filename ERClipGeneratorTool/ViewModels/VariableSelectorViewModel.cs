using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using ERClipGeneratorTool.Util;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ERClipGeneratorTool.ViewModels;

public class VariableSelectorViewModel : ViewModelBase
{
    private readonly IReadOnlyList<string> _variableNames;

    public VariableSelectorViewModel(IReadOnlyList<string> variableNames)
    {
        _variableNames = variableNames;

        SourceList<string> variableNameList = new();
        variableNameList.AddRange(_variableNames);

        IObservable<Func<string, bool>> variableFilter = this.WhenAnyValue(x => x.Filter)
            .Throttle(TimeSpan.FromMilliseconds(100))
            .Select(x => (Func<string, bool>)(y => y.Contains(x)));

        IObservable<IComparer<string>> sortComparer = this.WhenAnyValue(x => x.Filter)
            .Throttle(TimeSpan.FromMilliseconds(100))
            .Select(x => new FilteredStringComparer(x));

        FilteredVariables = new ObservableCollectionExtended<string>();
        variableNameList.Connect()
            .Filter(variableFilter)
            .Sort(sortComparer)
            .Bind(FilteredVariables)
            .Subscribe();

        IObservable<bool> isVariableSelected = this.WhenAnyValue(x => x.SelectedVariable).Select(x => x is not null);

        CancelCommand = ReactiveCommand.Create(Cancel);
        ConfirmCommand = ReactiveCommand.Create(Confirm, isVariableSelected);
    }

    public ReactiveCommand<Unit, int> CancelCommand { get; set; }
    public ReactiveCommand<Unit, int> ConfirmCommand { get; set; }

    [Reactive] public string Filter { get; set; } = "";
    [Reactive] public string? SelectedVariable { get; set; }
    public ObservableCollectionExtended<string> FilteredVariables { get; }

    private int Confirm()
    {
        return _variableNames.IndexOf(SelectedVariable);
    }

    private static int Cancel()
    {
        return -1;
    }
}