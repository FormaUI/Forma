namespace Forma.Examples.Maui;

using Android.App;
using Android.Runtime;

/// <summary>The application's Android side, which starts it.</summary>
/// <param name="handle">The Java object the application wraps.</param>
/// <param name="ownership">Who owns the Java object's reference.</param>
[Application]
public class MainApplication(IntPtr handle, JniHandleOwnership ownership) : MauiApplication(handle, ownership)
{
    /// <inheritdoc />
    protected override MauiApp CreateMauiApp()
        => MauiProgram.CreateMauiApp();
}
