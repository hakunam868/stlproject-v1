using ParseStl.Analysis;
using ParseStl.UI.Rendering;

namespace ParseStl.UI.Commands;

public sealed class AnalyseCommand(
    ApplicationSession session,
    IMeshAnalyzer analyzer,
    IReportRenderer renderer,
    IConsole console) : IMenuCommand
{
    public string Title => "Analyse the open file";

    public CommandResult Execute()
    {
        if (session.CurrentModel is null)
        {
            console.WriteLine("No file is open. Choose 'Open STL file' first.", ConsoleColor.Yellow);
            return CommandResult.Continue;
        }

        renderer.Render(analyzer.Analyze(session.CurrentModel));
        return CommandResult.Continue;
    }
}
