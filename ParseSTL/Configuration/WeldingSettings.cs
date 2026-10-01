namespace ParseStl.Configuration;

public sealed class WeldingSettings
{
    /// <summary>0 merges only bit-identical positions; a positive value snaps positions to a grid of this cell size.</summary>
    public double Tolerance { get; set; }
}
