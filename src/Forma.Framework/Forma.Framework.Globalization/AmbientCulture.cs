namespace Forma.Framework.Globalization;

using System.Globalization;

/// <summary>Runs synchronous operations with the specified ambient cultures.</summary>
public static class AmbientCulture
{
    /// <summary>Runs an operation in the given cultures and restores the caller's afterward.</summary>
    /// <typeparam name="TResult">The type of the operation's result.</typeparam>
    /// <param name="culture">The culture that formats dates, numbers and collation while the operation runs.</param>
    /// <param name="uiCulture">The culture that chooses strings and direction while the operation runs.</param>
    /// <param name="operation">The synchronous operation to run.</param>
    /// <returns>The operation's result.</returns>
    public static TResult Run<TResult>(CultureInfo culture, CultureInfo uiCulture, Func<TResult> operation)
    {
        ArgumentNullException.ThrowIfNull(culture);
        ArgumentNullException.ThrowIfNull(uiCulture);
        ArgumentNullException.ThrowIfNull(operation);

        if (IsTask(typeof(TResult)))
        {
            throw new ArgumentException("The operation must be synchronous and must not return a Task or ValueTask.", nameof(operation));
        }

        var callerCulture = CultureInfo.CurrentCulture;
        var callerUICulture = CultureInfo.CurrentUICulture;

        try
        {
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = uiCulture;

            return operation();
        }
        finally
        {
            CultureInfo.CurrentCulture = callerCulture;
            CultureInfo.CurrentUICulture = callerUICulture;
        }
    }

    /// <summary>Runs an operation in the given cultures and restores the caller's afterward.</summary>
    /// <param name="culture">The culture that formats dates, numbers and collation while the operation runs.</param>
    /// <param name="uiCulture">The culture that chooses strings and direction while the operation runs.</param>
    /// <param name="operation">The synchronous operation to run.</param>
    public static void Run(CultureInfo culture, CultureInfo uiCulture, Action operation)
    {
        ArgumentNullException.ThrowIfNull(culture);
        ArgumentNullException.ThrowIfNull(uiCulture);
        ArgumentNullException.ThrowIfNull(operation);

        Run(culture, uiCulture, () =>
        {
            operation();

            return true;
        });
    }

    private static bool IsTask(Type type)
        => typeof(Task).IsAssignableFrom(type)
            || type == typeof(ValueTask)
            || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ValueTask<>));
}
