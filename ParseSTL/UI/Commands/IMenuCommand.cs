namespace ParseStl.UI.Commands;

public enum CommandResult
{
    Continue,
    Exit,
}

/// <summary>One numbered entry in the main menu. Menu numbers follow the order in which commands are registered.</summary>
public interface IMenuCommand
{
    string Title { get; }

    CommandResult Execute();
}
