namespace Forma.Examples.Maui;

using Foundation;

/// <summary>The application's iOS side, which starts it.</summary>
[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    /// <inheritdoc />
    protected override MauiApp CreateMauiApp()
        => MauiProgram.CreateMauiApp();
}
