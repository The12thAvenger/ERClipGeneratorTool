using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.ReactiveUI;
using Avalonia.VisualTree;
using ERClipGeneratorTool.Controls;
using ERClipGeneratorTool.ViewModels;
using ReactiveHistory;
using ReactiveUI;
using ObservableExtensions = Reactive.Bindings.TinyLinq.ObservableExtensions;

namespace ERClipGeneratorTool.Views;

public class PropertyView<T> : ReactiveUserControl<PropertyViewModel<T>>
{
    private readonly ComboBox _bindingState = new();
    private readonly Button _changeVariable = new();
    private readonly TextBlock _name = new();
    private readonly CommitTextBox<T> _value = new();

    public PropertyView()
    {
        InitializeComponent();
        this.WhenActivated(d =>
        {
            ViewModel!.GetBindingVariableIndex.RegisterHandler(GetBindingVariableIndex).DisposeWith(d);

            _value.Text = ViewModel!.Value!.ToString();
            this.Bind(ViewModel,
                    viewModel => viewModel.Value,
                    view => view._value.CommittedValue)
                .DisposeWith(d);

            _bindingState.SelectedItem = BindingStateFromIsBound(ViewModel.IsBound);
            this.Bind(ViewModel,
                    viewModel => viewModel.IsBound,
                    view => view._bindingState.SelectedItem,
                    BindingStateFromIsBound,
                    IsBoundFromBindingState)
                .DisposeWith(d);

            this.BindCommand(ViewModel,
                    viewModel => viewModel.ChangeVariableIndexCommand,
                    view => view._changeVariable)
                .DisposeWith(d);

            ObservableExtensions.Select(this.WhenAnyValue(x => x.ViewModel!.IsBound), x => !x)
                .BindTo(_value, x => x.IsVisible)
                .DisposeWith(d);
            this.WhenAnyValue(x => x.ViewModel!.IsBound).BindTo(_changeVariable, x => x.IsVisible).DisposeWith(d);
            this.WhenAnyValue(x => x.ViewModel!.VariableName).BindTo((TextBlock)_changeVariable.Content!, x => x.Text)
                .DisposeWith(d);
            this.WhenAnyValue(x => x.ViewModel!.Name).BindTo(_name, x => x.Text).DisposeWith(d);
        });
    }

    private object BindingStateFromIsBound(bool isBound) => isBound ? BindingState.Binding : BindingState.Value;

    private bool IsBoundFromBindingState(object? bindingStateObj)
    {
        BindingState bindingState = (BindingState)(bindingStateObj ?? BindingStateFromIsBound(ViewModel!.IsBound));
        return bindingState.IsBound;
    }

    private async Task GetBindingVariableIndex(InteractionContext<VariableSelectorViewModel, int> interaction)
    {
        VariableSelectorView view = new()
        {
            DataContext = interaction.Input
        };
        interaction.SetOutput(await view.ShowDialog<int>(this.FindAncestorOfType<Window>()!));
    }

    private void InitializeComponent()
    {
        _name.VerticalAlignment = VerticalAlignment.Center;
        _bindingState.Items = BindingState.Values;
        _changeVariable.Content = new TextBlock();
        _changeVariable.BorderThickness = new Thickness(0.1);
        Grid grid = new()
        {
            ColumnDefinitions = new ColumnDefinitions("5*, 3*, 2*"),
            Children = { _name, _value, _changeVariable, _bindingState }
        };
        _name.SetValue(Grid.ColumnProperty, 0);
        _value.SetValue(Grid.ColumnProperty, 1);
        _changeVariable.SetValue(Grid.ColumnProperty, 1);
        _bindingState.SetValue(Grid.ColumnProperty, 2);
        Content = grid;
    }

    private record BindingState(string Name, bool IsBound)
    {
        public static readonly BindingState Value = new("Value", false);
        public static readonly BindingState Binding = new("Binding", true);

        public static readonly IReadOnlyList<BindingState> Values = new[]
        {
            Value,
            Binding
        };

        public override string ToString()
        {
            return Name;
        }
    }
}