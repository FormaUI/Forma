namespace Forma.Blazor.Components.Layout;

using Microsoft.AspNetCore.Components;

/// <summary>What the frame's script was attached with.</summary>
internal sealed record FrameAttachment(bool IsInteractive, FrameBox Box, ElementReference? Bounds);
