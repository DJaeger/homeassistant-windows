using System.Windows.Input;

namespace HAWindowsCompanion.App.Services;

public interface IMainWindowCommands
{
    ICommand RestartCommand { get; }
    ICommand QuitCommand { get; }
}
