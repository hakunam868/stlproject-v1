using System.Globalization;
using System.Numerics;

namespace ParseStl.UI;

internal static class Formatting
{
    public static string FileSize(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        _ => $"{bytes / (1024.0 * 1024.0):F1} MB",
    };

    public static string Duration(TimeSpan t) =>
        t.TotalSeconds >= 1 ? $"{t.TotalSeconds:F2} s" : $"{t.TotalMilliseconds:F1} ms";

    public static string Number(double value) =>
        double.IsPositiveInfinity(value) ? "∞" : value.ToString("G6", CultureInfo.InvariantCulture);

    public static string Vector(Vector3 v) => $"({Number(v.X)}, {Number(v.Y)}, {Number(v.Z)})";
}
