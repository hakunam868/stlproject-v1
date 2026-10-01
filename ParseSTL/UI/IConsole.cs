namespace ParseStl.UI;

/// <summary>Abstraction over the terminal so commands and renderers can be tested without a real console.</summary>
public interface IConsole
{
    void Write(string text, ConsoleColor? color = null);

    void WriteLine(string text = "", ConsoleColor? color = null);

    /// <summary>Returns null when input is closed (for example Ctrl+Z / Ctrl+D or redirected input ends).</summary>
    string? ReadLine();
}

public sealed class SystemConsole : IConsole
{
    public SystemConsole()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
    }

    public void Write(string text, ConsoleColor? color = null) => WithColor(color, () => Console.Write(text));

    public void WriteLine(string text = "", ConsoleColor? color = null) => WithColor(color, () => Console.WriteLine(text));

    public string? ReadLine() => Console.ReadLine();

    private static void WithColor(ConsoleColor? color, Action write)
    {
        if (color is null)
        {
            write();
            return;
        }

        var previous = Console.ForegroundColor;
        Console.ForegroundColor = color.Value;
        try
        {
            write();
        }
        finally
        {
            Console.ForegroundColor = previous;
        }
    }
}
