namespace Forma.Blazor.Components.Windows;

/// <summary>How a <see cref="FormaRestArea"/> clusters its handles.</summary>
public enum RestAreaGrouping
{
    /// <summary>One run of handles, in the order the windows registered.</summary>
    None,

    /// <summary>One cluster per application or feature, holding every instance's windows.</summary>
    Origin,

    /// <summary>One cluster per running instance of an application or feature.</summary>
    Instance,
}
