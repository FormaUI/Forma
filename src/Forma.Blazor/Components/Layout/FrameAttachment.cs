namespace Forma.Blazor.Components.Layout;

using Microsoft.AspNetCore.Components;

/// <summary>What the frame's script was attached with, compared between renders to tell a change that needs it attached again.</summary>
internal sealed record FrameAttachment(bool Interactive, FrameBox Box, ElementReference? Bounds);
