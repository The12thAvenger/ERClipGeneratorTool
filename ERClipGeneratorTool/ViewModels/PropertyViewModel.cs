using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using HKLib.hk2018;
using ReactiveHistory;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using ReactiveCommand = ReactiveUI.ReactiveCommand;

namespace ERClipGeneratorTool.ViewModels;

public class PropertyViewModel<T> : ViewModelBase, IActivatableViewModel
{
    public delegate ref T FieldSelector(hkbClipGenerator clipGenerator);

    private readonly hkbBehaviorGraph _behaviorGraph;
    private readonly hkbClipGenerator _clipGenerator;
    private readonly string _fieldName;
    private readonly FieldSelector _fieldSelector;
    private readonly IHistory _history;

    private bool _isBound;

    public PropertyViewModel(hkbBehaviorGraph behaviorGraph, hkbClipGenerator clipGenerator,
        FieldSelector fieldSelector, string fieldName, IHistory history)
    {
        _behaviorGraph = behaviorGraph;
        _clipGenerator = clipGenerator;
        _fieldName = fieldName;
        _fieldSelector = fieldSelector;
        _history = history;
        Activator = new ViewModelActivator();

        this.WhenActivated(d =>
        {
            List<string> variableNames = behaviorGraph.m_data!.m_stringData!.m_variableNames!;

            VariableIndex = -1;
            if (_clipGenerator.m_variableBindingSet?.m_bindings.Find(x => x.m_memberPath == _fieldName) is { } binding)
            {
                _isBound = true;
                VariableIndex = binding.m_variableIndex;
            }

            UpdateBindingCommand = ReactiveCommand.CreateFromTask<bool>(UpdateBindingAsync).DisposeWith(d);
            ChangeVariableIndexCommand = ReactiveCommand.CreateFromTask(ChangeVariableIndexAsync).DisposeWith(d);

            this.WhenAnyValue(x => x.Value).ObserveWithHistory(value => Value = value,
                _fieldSelector(_clipGenerator), history);
            this.WhenAnyValue(x => x.VariableIndex).Select(x => x < 0 ? "None" : variableNames[x])
                .ToPropertyEx(this, x => x.VariableName,
                    VariableIndex < 0 ? "None" : variableNames[VariableIndex]);

            // skip update for initial value
            this.WhenAnyValue(x => x.IsBound).Skip(1).InvokeCommand(UpdateBindingCommand);
        });
    }


    public string Name => GetDisplayName(_fieldName);

    public T Value
    {
        get => _fieldSelector(_clipGenerator);
        set => this.RaiseAndSetIfChanged(ref _fieldSelector(_clipGenerator), value);
    }

    public bool IsBound
    {
        get => _isBound;
        set => this.RaiseAndSetIfChanged(ref _isBound, value);
    }

    [Reactive] private int VariableIndex { get; set; }
    [ObservableAsProperty] public string? VariableName { get; }

    public ReactiveCommand<bool, Unit> UpdateBindingCommand { get; private set; } = null!;
    public ReactiveCommand<Unit, Unit> ChangeVariableIndexCommand { get; private set; } = null!;

    public Interaction<VariableSelectorViewModel, int> GetBindingVariableIndex { get; } = new();

    public ViewModelActivator Activator { get; }

    private string GetDisplayName(string fieldName)
    {
        StringBuilder stringBuilder = new(fieldName[0].ToString().ToUpper());
        foreach (char c in fieldName[1..])
        {
            if (char.IsUpper(c)) stringBuilder.Append(' ');
            stringBuilder.Append(c);
        }

        return stringBuilder.ToString();
    }

    private async Task<Unit> UpdateBindingAsync(bool isBound)
    {
        // VariableIndex signifies whether update has been completed
        if (!isBound)
        {
            if (VariableIndex != -1) UnbindFromVariableWithHistory();
            return Unit.Default;
        }

        if (VariableIndex != -1) return Unit.Default;

        int variableIndex = await GetVariableIndexAsync();
        if (variableIndex < 0)
        {
            IsBound = false;
            return Unit.Default;
        }

        BindToVariableWithHistory(variableIndex);
        return Unit.Default;
    }

    private void BindToVariableWithHistory(int variableIndex)
    {
        int prevVariableIndex = VariableIndex;
        _history.Snapshot(Undo, Redo);
        Redo();

        void Redo()
        {
            BindToVariable(variableIndex);
        }

        void Undo()
        {
            if (prevVariableIndex > 0)
            {
                BindToVariable(prevVariableIndex);
            }
            else
            {
                UnbindFromVariable();
            }
        }
    }

    private void UnbindFromVariableWithHistory()
    {
        int prevVariableIndex = VariableIndex;
        _history.Snapshot(Undo, Redo);
        Redo();

        void Redo()
        {
            UnbindFromVariable();
        }

        void Undo()
        {
            BindToVariable(prevVariableIndex);
        }
    }

    private void BindToVariable(int variableIndex)
    {
        VariableIndex = variableIndex;
        IsBound = true;
        _clipGenerator.m_variableBindingSet ??= new hkbVariableBindingSet();
        if (_clipGenerator.m_variableBindingSet.m_bindings.Find(x => x.m_memberPath == _fieldName) is { } binding)
        {
            binding.m_variableIndex = variableIndex;
            return;
        }

        _clipGenerator.m_variableBindingSet.m_bindings.Add(new hkbVariableBindingSet.Binding
        {
            m_bindingType = hkbVariableBindingSet.Binding.BindingType.BINDING_TYPE_VARIABLE,
            m_bitIndex = -1,
            m_memberPath = _fieldName,
            m_variableIndex = variableIndex
        });
    }

    private void UnbindFromVariable()
    {
        _clipGenerator.m_variableBindingSet?.m_bindings.RemoveAll(x => x.m_memberPath == _fieldName);
        if (_clipGenerator.m_variableBindingSet?.m_bindings.Count == 0) _clipGenerator.m_variableBindingSet = null;
        VariableIndex = -1;
        IsBound = false;
    }

    private async Task ChangeVariableIndexAsync()
    {
        int variableIndex = await GetVariableIndexAsync();
        if (variableIndex < 0) return;

        BindToVariableWithHistory(variableIndex);
    }

    private async Task<int> GetVariableIndexAsync()
    {
        VariableSelectorViewModel vm = new(_behaviorGraph.m_data!.m_stringData!.m_variableNames!);
        return await GetBindingVariableIndex.Handle(vm);
    }
}