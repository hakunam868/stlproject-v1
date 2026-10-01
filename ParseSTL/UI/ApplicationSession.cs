using ParseStl.Model;

namespace ParseStl.UI;

/// <summary>State shared between menu commands: the currently open model.</summary>
public sealed class ApplicationSession
{
    public LoadedModel? CurrentModel { get; set; }
}
