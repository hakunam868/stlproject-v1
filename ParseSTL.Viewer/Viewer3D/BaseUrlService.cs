namespace ParseSTL.Viewer.Viewer3D;

/// <summary>
/// Stand-in for Maker.Core.SubPage.BaseUrlService. In Maker the viewer assets live in the
/// Maker.Assets.Razor library (/_content/Maker.Assets.Razor/); here they sit in this app's
/// own wwwroot. Must match the MakerBaseUrl meta tag in wwwroot/index.html.
/// </summary>
public static class BaseUrlService
{
    public static string GetBaseUrl() => "./";
}
