namespace Forma.Blazor.Components.Windows;

using Forma.Framework.Icons;
using Forma.Framework.Windows;

/// <summary>What a window declares to its workspace, compared between renders to tell a change worth refreshing from a render that changed nothing.</summary>
internal sealed record WindowDeclaration(
    string Title,
    Icon? Icon,
    Guid? ParentId,
    WindowOrigin? Origin,
    WindowOrigin? CascadedOrigin,
    WindowMode Mode,
    bool Minimized,
    bool Disabled,
    bool Modal);
