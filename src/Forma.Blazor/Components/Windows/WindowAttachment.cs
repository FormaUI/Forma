namespace Forma.Blazor.Components.Windows;

using Microsoft.AspNetCore.Components;

/// <summary>What the window's script was attached with, compared between renders to tell a change that needs it attached again.</summary>
internal sealed record WindowAttachment(WindowMode Mode, bool Modal, ElementReference? Bounds, bool Backdrop);
