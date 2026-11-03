namespace Forma.Examples.Maui;

/// <summary>The application's native windows, which a page opens another of.</summary>
/// <param name="services">The application's services, which every web view shares.</param>
public sealed class NativeWindows(IServiceProvider services)
{
    /// <summary>Opens another native window, its web view sharing the application's services.</summary>
    public void Open()
        => Application.Current!.OpenWindow(new MainWindow(services));
}
