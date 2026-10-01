using ParseStl.UI.Commands;

namespace ParseStl.UI;

/// <summary>Main loop: shows the numbered menu and runs the chosen command until a command returns Exit.</summary>
public sealed class MenuApplication(IReadOnlyList<IMenuCommand> commands, ApplicationSession session, INumberPrompt prompt, IConsole console)
{
    public void Run()
    {
        console.WriteLine("STL Parser & Analyser", ConsoleColor.White);

        while (true)
        {
            console.WriteLine();
            console.WriteLine("----------------------------------------", ConsoleColor.DarkGray);
            var current = session.CurrentModel;
            console.WriteLine(current is null ? "Open file: (none)" : $"Open file: {current.FileName}", ConsoleColor.DarkGray);
            for (var i = 0; i < commands.Count; i++)
                console.WriteLine($"  {i + 1}. {commands[i].Title}");

            var choice = prompt.Ask("Choose an option", 1, commands.Count);
            if (choice is null)
                return; // input closed

            if (commands[choice.Value - 1].Execute() == CommandResult.Exit)
                return;
        }
    }
}
