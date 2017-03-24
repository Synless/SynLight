using System;
using System.Windows.Input;

namespace SynLight.ViewModel
{
    public class Command : ICommand
    {
        readonly Action<object> actionAExecuter;

        public Command(Action<object> execute)
            : this(execute, null)
        {
        }
        public Command(Action<object> execute, Predicate<object> canExecute)
        {
            actionAExecuter = execute;
        }
        public bool CanExecute(object parameter)
        {
            return true;
        }

        public event EventHandler CanExecuteChanged;
        public void Execute(object parameter)
        {
            actionAExecuter(parameter);
        }
    }
}
