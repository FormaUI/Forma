namespace Forma.Blazor.Components.DataView;

/// <summary>What a data view's request asks its provider for.</summary>
public enum DataViewRequestKind
{
    /// <summary>The content of the current node, or of the roots, which the views beside the tree show.</summary>
    Content,

    /// <summary>A node's children, or the roots, which the tree shows a level at a time.</summary>
    Children
}
