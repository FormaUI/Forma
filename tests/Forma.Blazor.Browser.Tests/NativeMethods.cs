namespace Forma.Blazor.Browser.Tests;

using System.Runtime.InteropServices;

/// <summary>The Win32 calls the checks make on an application's native windows.</summary>
internal static partial class NativeMethods
{
    /// <summary>The next top-level native window after <paramref name="after"/> whose class and title match.</summary>
    /// <param name="parent">The parent, zero for the desktop's top-level native windows.</param>
    /// <param name="after">The native window the search continues after, zero to start from the first.</param>
    /// <param name="className">The class, any where none is named.</param>
    /// <param name="title">The title, any where none is named.</param>
    /// <returns>The native window's handle, zero once none is left.</returns>
    [LibraryImport("user32.dll", EntryPoint = "FindWindowExW", StringMarshalling = StringMarshalling.Utf16)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static partial nint FindWindowEx(nint parent, nint after, string? className, string? title);

    /// <summary>The thread that created the native window, and the process it belongs to.</summary>
    /// <param name="window">The native window's handle.</param>
    /// <param name="processId">The process the native window belongs to.</param>
    /// <returns>The thread's identifier.</returns>
    [LibraryImport("user32.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static partial uint GetWindowThreadProcessId(nint window, out uint processId);

    /// <summary>Whether the native window shows.</summary>
    /// <param name="window">The native window's handle.</param>
    /// <returns>Whether it shows.</returns>
    [LibraryImport("user32.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool IsWindowVisible(nint window);

    /// <summary>Whether the native window keeps its text as Unicode, rather than in the system's ANSI code page.</summary>
    /// <param name="window">The native window's handle.</param>
    /// <returns>Whether it keeps Unicode.</returns>
    [LibraryImport("user32.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool IsWindowUnicode(nint window);

    /// <summary>The system's ANSI code page.</summary>
    /// <returns>The code page's identifier.</returns>
    [LibraryImport("kernel32.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static partial uint GetACP();

    /// <summary>The native window's title, copied into <paramref name="title"/>.</summary>
    /// <param name="window">The native window's handle.</param>
    /// <param name="title">The buffer the title is copied into.</param>
    /// <param name="length">The buffer's length, its terminating null included.</param>
    /// <returns>How many characters were copied.</returns>
    [LibraryImport("user32.dll", EntryPoint = "GetWindowTextW")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static unsafe partial int GetWindowText(nint window, char* title, int length);

    /// <summary>Posts <paramref name="message"/> to the native window's queue.</summary>
    /// <param name="window">The native window's handle.</param>
    /// <param name="message">The message.</param>
    /// <param name="wParam">The message's first parameter.</param>
    /// <param name="lParam">The message's second parameter.</param>
    /// <returns>Whether the message was posted.</returns>
    [LibraryImport("user32.dll", EntryPoint = "PostMessageW", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool PostMessage(nint window, uint message, nint wParam, nint lParam);
}
