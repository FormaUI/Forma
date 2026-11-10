namespace Forma.Blazor.Components.DataView;

/// <summary>Who arranges a data view's views and its footer.</summary>
public enum DataViewLayout
{
    /// <summary>The data view arranges them: a companion view beside the active one, stacked above it where the data view is narrow, and the footer beneath both.</summary>
    BuiltIn,

    /// <summary>The host arranges them inside the data view: a companion view fills the region the host gives it, and the footer stands where the host places a <see cref="FormaDataViewFooter"/>.</summary>
    Host,
}
