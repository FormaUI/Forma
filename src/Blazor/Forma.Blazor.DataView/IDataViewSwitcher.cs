namespace Forma.Blazor.Components.DataView;

using Forma.Framework.Events;

/// <summary>What a view switch reads and drives of a data view: the views registered with it, the one showing, and switching to another, announced through <see cref="INotifyStateChanged"/> as it changes.</summary>
public interface IDataViewSwitcher : INotifyStateChanged
{
    /// <summary>The views registered, in the order declared.</summary>
    IReadOnlyList<DataViewRegistration> Views { get; }

    /// <summary>The key of the view showing.</summary>
    string? ActiveViewKey { get; }

    /// <summary>Shows the view registered under <paramref name="key"/>.</summary>
    /// <param name="key">The view's key.</param>
    Task SwitchViewAsync(string key);
}
