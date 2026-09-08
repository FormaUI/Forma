namespace Forma.Blazor.Components.Windows;

/// <summary>What the window's script was attached with, compared between renders to tell a change that needs it attached again.</summary>
internal sealed record WindowAttachment(bool Modal, bool Scrim);
