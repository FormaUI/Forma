namespace Forma.Features.Explorer;

using Forma.Framework.Resources;

/// <summary>Names a resource's kind for a reader.</summary>
public interface IResourceKindLabels
{
    /// <summary>The kind label of the given resource.</summary>
    string LabelOf(ResourceInfo resource);
}
