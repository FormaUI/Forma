namespace Forma.Examples.Maui.WinUI;

/// <summary>The application's Windows side, which starts it.</summary>
public partial class App : MauiWinUIApplication
{
    /// <summary>Creates the application's Windows side.</summary>
    public App()
        => InitializeComponent();

    /// <inheritdoc />
    protected override MauiApp CreateMauiApp()
        => MauiProgram.CreateMauiApp();
}
