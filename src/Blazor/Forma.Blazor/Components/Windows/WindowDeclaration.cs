namespace Forma.Blazor.Components.Windows;

using Forma.Framework.Icons;
using Forma.Framework.Windows;

/// <summary>What a window declares to its workspace.</summary>
internal sealed record WindowDeclaration(
    string Title,
    Icon? Icon,
    Guid? ParentId,
    WindowOrigin? Origin,
    WindowOrigin? CascadedOrigin,
    bool IsMinimized,
    bool IsDisabled,
    bool IsModal);
