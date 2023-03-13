using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Styling;

namespace ERClipGeneratorTool.Controls;

// Adapted from Avalonia.Diagnostics
// TextBox which continously validates the input but only propagates it when losing focus or when the user presses enter.
// Invalid values are never propagated, instead the content of the TextBox is reset to the last valid value.
public class CommitTextBox<T> : TextBox, IStyleable
{
    /// <summary>
    /// Defines the <see cref="CommittedValue" /> property.
    /// </summary>
    public static readonly DirectProperty<CommitTextBox<T>, T?> CommittedValueProperty =
        AvaloniaProperty.RegisterDirect<CommitTextBox<T>, T?>(
            nameof(CommittedValue), o => o.CommittedValue, (o, v) => o.CommittedValue = v);

    private T? _committedValue;

    public T? CommittedValue
    {
        get => _committedValue;
        set => SetAndRaise(CommittedValueProperty, ref _committedValue, value);
    }

    public ValidationContext? ValidationContext { get; set; }

    Type IStyleable.StyleKey => typeof(TextBox);

    public override void EndInit()
    {
        base.EndInit();
        Text = ConvertValueToText(CommittedValue);
    }

    protected virtual T? ConvertTextToValue(string? text)
    {
        return (T?)Convert.ChangeType(text, typeof(T));
    }

    protected virtual string? ConvertValueToText(T? value)
    {
        return value?.ToString();
    }

    protected virtual BindingNotification ValidateText(string? text)
    {
        try
        {
            object? value = Convert.ChangeType(text, typeof(T));
            if (ValidationContext is not null)
            {
                List<ValidationResult> validationResults = new();
                if (!Validator.TryValidateProperty(value, ValidationContext, validationResults))
                    return new BindingNotification(new DataValidationException(validationResults[0].ErrorMessage),
                        BindingErrorType.Error);
            }

            return BindingNotification.Null;
        }
        catch (Exception)
        {
            return new BindingNotification(new DataValidationException($"Value could not be converted to {typeof(T)}"),
                BindingErrorType.Error);
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == CommittedValueProperty)
        {
            Text = ConvertValueToText(CommittedValue);
        }

        if (change.Property == TextProperty)
        {
            if (ValidateText(Text).Error is { } e)
            {
                DataValidationErrors.SetError(this, e);
            }
            else
            {
                DataValidationErrors.ClearErrors(this);
            }
        }
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);

        switch (e.Key)
        {
            case Key.Enter:

                TryCommit();

                e.Handled = true;

                break;

            case Key.Escape:

                Cancel();

                e.Handled = true;

                break;
        }
    }

    protected override void OnLostFocus(RoutedEventArgs e)
    {
        base.OnLostFocus(e);

        TryCommit();
    }

    private void Cancel()
    {
        Text = ConvertValueToText(CommittedValue);
        DataValidationErrors.ClearErrors(this);

        // remove focus
        IsEnabled = false;
        IsEnabled = true;
    }

    private void TryCommit()
    {
        if (!DataValidationErrors.GetHasErrors(this))
        {
            CommittedValue = ConvertTextToValue(Text);
        }
        else
        {
            Text = ConvertValueToText(CommittedValue);
            DataValidationErrors.ClearErrors(this);
        }

        // remove focus
        IsEnabled = false;
        IsEnabled = true;
    }
}