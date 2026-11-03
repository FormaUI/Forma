namespace Forma.Blazor.Browser.Android.Platforms.Android;

using global::Android.App;
using global::Android.Runtime;

/// <summary>The check host's Android side, which starts it.</summary>
/// <param name="handle">The Java object the application wraps.</param>
/// <param name="ownership">Who owns the Java object's reference.</param>
[Application]
public class MainApplication(IntPtr handle, JniHandleOwnership ownership) : MauiApplication(handle, ownership)
{
    /// <inheritdoc />
    protected override MauiApp CreateMauiApp()
        => MauiProgram.CreateMauiApp();
}
