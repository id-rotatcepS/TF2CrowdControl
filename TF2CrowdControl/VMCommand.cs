using System.Windows.Input;

namespace TF2CrowdControl
{
    /// <summary>
    /// Alternative to RelayCommand specific to this VM for commands that need its instance.
    /// </summary>
    public abstract class VMCommand : ICommand
    {
        public abstract void Execute(object? arg);
        public abstract bool CanExecute(object? arg);
        internal readonly TF2CrowdControlViewModel vm;

        internal VMCommand(TF2CrowdControlViewModel vm)
        {
            this.vm = vm;
        }

        ///<summary>
        ///Occurs when changes occur that affect whether or not the command should execute.
        ///</summary>
        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }
}
