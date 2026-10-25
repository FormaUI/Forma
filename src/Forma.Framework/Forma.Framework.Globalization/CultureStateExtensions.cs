namespace Forma.Framework.Globalization;

/// <summary>Runs a synchronous operation that reads the ambient culture in a culture state's cultures.</summary>
public static class CultureStateExtensions
{
    /// <summary>Runs an operation in the state's cultures and restores the caller's afterward.</summary>
    /// <typeparam name="TResult">The type of the operation's result.</typeparam>
    /// <param name="cultureState">The culture state whose cultures the operation runs in.</param>
    /// <param name="operation">The synchronous operation to run.</param>
    /// <returns>The operation's result.</returns>
    public static TResult Run<TResult>(this ICultureState cultureState, Func<TResult> operation)
    {
        ArgumentNullException.ThrowIfNull(cultureState);

        return AmbientCulture.Run(cultureState.Culture, cultureState.UICulture, operation);
    }

    /// <summary>Runs an operation in the state's cultures and restores the caller's afterward.</summary>
    /// <param name="cultureState">The culture state whose cultures the operation runs in.</param>
    /// <param name="operation">The synchronous operation to run.</param>
    public static void Run(this ICultureState cultureState, Action operation)
    {
        ArgumentNullException.ThrowIfNull(cultureState);

        AmbientCulture.Run(cultureState.Culture, cultureState.UICulture, operation);
    }
}
