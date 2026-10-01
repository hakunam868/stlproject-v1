namespace ParseStl.Files;

public interface IStlFileRepository
{
    string FolderPath { get; }

    /// <summary>The *.stl files in the folder, sorted by name. Creates the folder if it does not exist.</summary>
    IReadOnlyList<FileInfo> GetFiles();
}

public sealed class StlFileRepository(string folderPath) : IStlFileRepository
{
    public string FolderPath { get; } = folderPath;

    public IReadOnlyList<FileInfo> GetFiles()
    {
        var directory = Directory.CreateDirectory(FolderPath);
        return directory
            .EnumerateFiles("*", new EnumerationOptions { MatchCasing = MatchCasing.CaseInsensitive, IgnoreInaccessible = true })
            .Where(f => f.Extension.Equals(".stl", StringComparison.OrdinalIgnoreCase))
            .OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
