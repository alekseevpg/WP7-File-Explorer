using System;
using System.Windows.Input;

namespace SPBTV_TestApp.Libraries
{
    public class DelegateCommand : ICommand
    {
        private readonly Func<object, bool> canExecute;

        private readonly Func<bool> canExecuteSimple;
        private readonly Action<object> executeAction;
        private readonly Action executeActionSimple;

        public DelegateCommand(Action<object> executeAction)
            : this(executeAction, null)
        {
        }

        public DelegateCommand(Action executeAction)
            : this(executeAction, null)
        {
        }

        public DelegateCommand(Action<object> executeAction, Func<object, bool> canExecute)
        {
            if (executeAction == null)
            {
                throw new ArgumentNullException("executeAction");
            }
            this.executeAction = executeAction;
            this.canExecute = canExecute;
        }

        public DelegateCommand(Action executeAction, Func<bool> canExecute)
        {
            if (executeAction == null)
            {
                throw new ArgumentNullException("executeAction");
            }
            executeActionSimple = executeAction;
            canExecuteSimple = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            bool result = true;
            Func<object, bool> canExecuteHandler = canExecute;
            if (canExecuteHandler != null)
            {
                result = canExecuteHandler(parameter);
                return result;
            }

            Func<bool> canExecuteHandlerSimple = canExecuteSimple;
            if (canExecuteHandlerSimple != null)
            {
                result = canExecuteHandlerSimple();
            }

            return result;
        }


        public event EventHandler CanExecuteChanged;

        public void Execute(object parameter)
        {
            // Default to action that takes parameter.
            if (executeAction != null)
            {
                executeAction(parameter);
                return;
            }

            // Fallback to parameterless delegate.
            if (executeActionSimple != null)
            {
                executeActionSimple();
            }
        }

        public bool CanExecute()
        {
            bool result = true;
            Func<bool> canExecuteHandler = canExecuteSimple;
            if (canExecuteHandler != null)
            {
                result = canExecuteHandler();
            }

            return result;
        }

        public void RaiseCanExecuteChanged()
        {
            EventHandler handler = CanExecuteChanged;
            if (handler != null)
            {
                handler(this, new EventArgs());
            }
        }

        public void Execute()
        {
            executeActionSimple();
        }
    }
}