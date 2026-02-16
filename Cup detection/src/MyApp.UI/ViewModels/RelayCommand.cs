using System;
using System.Windows.Input;

namespace MyApp.UI.ViewModels;

public class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

    public void Execute(object? parameter) => _execute();

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    public void RaiseCanExecuteChangedOnUI()
    {
        try
        {
            var app = System.Windows.Application.Current;
            if (app != null)
            {
                app.Dispatcher.Invoke(() => CanExecuteChanged?.Invoke(this, EventArgs.Empty));
                return;
            }
        }
        catch
        {
            // ignore and fall back to direct invoke
        }

        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}