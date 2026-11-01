namespace Forma.Blazor.Browser.Tests;

using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

/// <summary>An application's native windows, found by the process they belong to, read by their title and closed as a reader closes one.</summary>
internal static class NativeWindows
{
    private const uint CloseMessage = 0x0010;

    /// <summary>The titled top-level native windows the process <paramref name="processId"/> names shows.</summary>
    /// <param name="processId">The process the native windows belong to.</param>
    /// <returns>The native windows' handles.</returns>
    public static IReadOnlyList<nint> Find(int processId)
    {
        var windows = new List<nint>();
        var window = nint.Zero;

        while ((window = NativeMethods.FindWindowEx(nint.Zero, window, null, null)) != nint.Zero)
        {
            if (NativeMethods.GetWindowThreadProcessId(window, out var owner) != 0 && owner == (uint)processId && NativeMethods.IsWindowVisible(window) && GetTitle(window).Length > 0)
            {
                windows.Add(window);
            }
        }

        return windows;
    }

    /// <summary>The native window's title.</summary>
    /// <param name="window">The native window's handle.</param>
    /// <returns>The title.</returns>
    public static unsafe string GetTitle(nint window)
    {
        Span<char> title = stackalloc char[512];

        fixed (char* buffer = title)
        {
            return new string(title[..NativeMethods.GetWindowText(window, buffer, title.Length)]);
        }
    }

    /// <summary><paramref name="title"/> as the native window keeps it: whole where it keeps Unicode, and otherwise in the system's ANSI code page, which writes a character it lacks as a question mark.</summary>
    /// <param name="window">The native window's handle.</param>
    /// <param name="title">The title.</param>
    /// <returns>The title as the native window keeps it.</returns>
    public static string AsKept(nint window, string title)
    {
        if (NativeMethods.IsWindowUnicode(window))
        {
            return title;
        }

        var codePage = CodePagesEncodingProvider.Instance.GetEncoding((int)NativeMethods.GetACP()) ?? Encoding.GetEncoding((int)NativeMethods.GetACP());

        return codePage.GetString(codePage.GetBytes(title));
    }

    /// <summary>Asks the native window to close, as its close button does.</summary>
    /// <param name="window">The native window's handle.</param>
    public static void Close(nint window)
    {
        if (!NativeMethods.PostMessage(window, CloseMessage, 0, 0))
        {
            throw new Win32Exception(Marshal.GetLastPInvokeError());
        }
    }
}
