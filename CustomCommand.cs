using System;
using System.Windows.Input;

namespace PageSourceUI
{
    public class CustomCommand : ICommand
    {
        private readonly Action _executeMethod;

        public CustomCommand(Action executeMethod)
        {
            _executeMethod = executeMethod ?? throw new ArgumentNullException(nameof(executeMethod));
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            _executeMethod?.Invoke();
        }

        public event EventHandler CanExecuteChanged
        {
            add
            {
                CommandManager.RequerySuggested += value;
            }
            remove
            {
                CommandManager.RequerySuggested -= value;
            }
        }
    }
}
