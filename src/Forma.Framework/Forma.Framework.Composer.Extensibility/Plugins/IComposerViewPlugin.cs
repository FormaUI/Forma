namespace Forma.Framework.Composer.Plugins;

using System.Diagnostics.CodeAnalysis;

using Forma.Framework.Icons;

/// <summary>A read-only way of looking at one editor's document, such as a rendered preview or its source, with a command bar of its own.</summary>
public interface IComposerViewPlugin : IComposerPlugin
{
    /// <summary>The kind of view, one of <see cref="ComposerViews"/>.</summary>
    string Kind { get; }

    /// <summary>The title of the view, in English, used as the localization key.</summary>
    string Title { get; }

    /// <summary>The icon associated with the view.</summary>
    Icon Icon { get; }

    /// <summary>The component filling this view's canvas.</summary>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    Type ViewComponent { get; }
}
