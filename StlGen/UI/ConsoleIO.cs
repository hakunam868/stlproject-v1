using System.Globalization;

namespace StlGen.UI;

public interface IConsole
{
    void Write(string text, ConsoleColor? color = null);

    void WriteLine(string text = "", ConsoleColor? color = null);

    /// <summary>Returns null when input is closed.</summary>
    string? ReadLine();
}

public sealed class SystemConsole : IConsole
{
    public SystemConsole() => Console.OutputEncoding = System.Text.Encoding.UTF8;

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

public interface IPrompt
{
    /// <summary>Asks for a whole number in [min, max]. Returns null when input is closed.</summary>
    int? Choice(string label, int min, int max, int? @default = null);

    /// <summary>Asks for a number. An empty answer keeps <paramref name="default"/>. Returns null when input is closed.</summary>
    double? Number(string label, double @default, double min, double max, bool integer);
}

public sealed class Prompt(IConsole console) : IPrompt
{
    public int? Choice(string label, int min, int max, int? @default = null)
    {
        var value = Ask($"{label} [{min}-{max}]" + (@default is { } d ? $" (Enter = {d})" : ""), @default, min, max, integer: true);
        return value is null ? null : (int)value.Value;
    }

    public double? Number(string label, double @default, double min, double max, bool integer) =>
        Ask($"{label} [{@default.ToString("G", CultureInfo.InvariantCulture)}]", @default, min, max, integer);

    private double? Ask(string text, double? @default, double min, double max, bool integer)
    {
        while (true)
        {
            console.Write($"{text}: ", ConsoleColor.Cyan);
            var input = console.ReadLine();
            if (input is null)
                return null;

            input = input.Trim();
            if (input.Length == 0 && @default is { } d)
                return d;

            if (double.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
                && value >= min && value <= max && (!integer || value == Math.Floor(value)))
                return value;

            console.WriteLine($"  Please enter {(integer ? "a whole number" : "a number")} between {min:G} and {max:G}.", ConsoleColor.Yellow);
        }
    }
}
