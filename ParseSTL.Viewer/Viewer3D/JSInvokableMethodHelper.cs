using Microsoft.JSInterop;
using System.Reflection;

namespace ParseSTL.Viewer.Viewer3D;

/// <summary>Copy of Maker.Core.SubPage.JSInvokableMethodHelper, without the plugin constraint.</summary>
public static class JSInvokableMethodHelper
{
    public static string[] GetJSInvokableMethods<T>() where T : class
    {
        // Use reflection to get all methods with the JSInvokable attribute
        return typeof(T)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.GetCustomAttributes(typeof(JSInvokableAttribute), false).Any())
            .Select(m => m.Name)
            .ToArray();
    }
}
