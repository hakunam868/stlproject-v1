namespace ParseStl.UI.Commands;

public sealed class ExitCommand(IConsole console) : IMenuCommand
{
    public string Title => "Close the application";

    public CommandResult Execute()
    {
        console.WriteLine("Goodbye.");
        return CommandResult.Exit;
    }
}
