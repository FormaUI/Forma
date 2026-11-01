namespace Forma.Examples.Maui;

/// <summary>The application, opening its first native window.</summary>
public partial class App : Application
{
    private readonly IServiceProvider _services;

    /// <summary>Creates the application.</summary>
    /// <param name="services">The application's services, which every web view shares.</param>
    public App(IServiceProvider services)
    {
        InitializeComponent();
        _services = services;
    }

    /// <inheritdoc />
    protected override Window CreateWindow(IActivationState? activationState)
        => new MainWindow(_services);
}
