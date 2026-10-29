using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SystemMonitor.Commands
{
    internal class AsyncRelayCommand : ICommand
    {
        private readonly Func<Task> _executeAsync;
        private readonly Action<Exception> _onError;

        private bool _isExecuting;

        public AsyncRelayCommand(Func<Task> executeAsync, Action<Exception> onError)
        {
            _executeAsync = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));

            _onError = onError ?? throw new ArgumentNullException(nameof(onError));
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter)
        {
            return !_isExecuting;
        }

        public async void Execute(object? parameter)
        {
            if(!CanExecute(parameter))
            {
                return;
            }

            _isExecuting = true;
            RaiseCanExecuteChanged();

            try
            {
                await _executeAsync();
            }
            catch (Exception exception) 
            {
                _onError(exception);
            }
            finally
            {
                _isExecuting = false;
                RaiseCanExecuteChanged();
            }
        }

        private void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
