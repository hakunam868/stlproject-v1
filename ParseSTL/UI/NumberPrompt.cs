namespace ParseStl.UI;

public interface INumberPrompt
{
    /// <summary>Asks until the user enters a whole number in [min, max]. Returns null if input is closed.</summary>
    int? Ask(string prompt, int min, int max);
}

public sealed class NumberPrompt(IConsole console) : INumberPrompt
{
    public int? Ask(string prompt, int min, int max)
    {
        while (true)
        {
            console.Write($"{prompt} [{min}-{max}]: ", ConsoleColor.Cyan);
            var input = console.ReadLine();
            if (input is null)
                return null;

            if (int.TryParse(input.Trim(), out var value) && value >= min && value <= max)
                return value;

            console.WriteLine($"  Please enter a number between {min} and {max}.", ConsoleColor.Yellow);
        }
    }
}
