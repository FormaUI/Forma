namespace Forma.UI.Blazor.Tests;

using Forma.UI.Blazor.Icons;

/// <summary>A bUnit context with the services every Forma component expects already registered.</summary>
public abstract class FormaBunitContext : BunitContext
{
    /// <summary>Creates a context carrying Forma's own service registrations.</summary>
    protected FormaBunitContext()
    {
        Services.AddFormaIcons();
    }
}
