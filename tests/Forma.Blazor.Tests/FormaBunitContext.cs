namespace Forma.Blazor.Tests;

using Forma.Blazor;
using Forma.Framework.Emojis.Twemoji;

/// <summary>A bUnit context with the services every Forma component expects already registered.</summary>
public abstract class FormaBunitContext : BunitContext
{
    /// <summary>Creates a context carrying Forma's own service registrations.</summary>
    protected FormaBunitContext()
    {
        Services.AddFormaTwemojiPack();
        Services.AddFormaUI();
    }
}
