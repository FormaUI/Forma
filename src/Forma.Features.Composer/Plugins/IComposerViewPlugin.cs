namespace Forma.Features.Composer.Plugins;

using Forma.Framework.Icons;

/// <summary>A read-only way of looking at one editor's document, such as a rendered preview or its source, with a command bar of its own.</summary>
public interface IComposerViewPlugin : IComposerPlugin
{
    /// <summary>The identifier of the editor this view belongs to.</summary>
    string EditorId { get; }

    /// <summary>The kind of view, one of <see cref="ComposerViews"/>.</summary>
    string Kind { get; }

    /// <summary>The title of the view, in English, used as the localization key.</summary>
    string Title { get; }

    /// <summary>The icon associated with the state.</summary>
    Icon Icon { get; }

    /// <summary>The component filling this view's canvas.</summary>
    Type ViewComponent { get; }
}
