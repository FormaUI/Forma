namespace Forma.Tests.Hosts;

using System.Runtime.InteropServices;

/// <summary>The Win32 calls that put a started host in a job of the check's own process.</summary>
internal static partial class NativeMethods
{
    /// <summary>The job limit that ends every process in the job once the job's last handle closes.</summary>
    internal const uint JobObjectLimitKillOnJobClose = 0x2000;

    /// <summary>The class of information <see cref="SetInformationJobObject"/> sets, a job's extended limits.</summary>
    internal const int JobObjectExtendedLimitInformationClass = 9;

    /// <summary>Creates a job, nameless and with the default security.</summary>
    /// <param name="attributes">The job's security attributes, zero for the default.</param>
    /// <param name="name">The job's name, none for a job of the process's own.</param>
    /// <returns>The job's handle, zero where it wasn't created.</returns>
    [LibraryImport("kernel32.dll", EntryPoint = "CreateJobObjectW", StringMarshalling = StringMarshalling.Utf16)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static partial nint CreateJobObject(nint attributes, string? name);

    /// <summary>Sets the job's extended limits.</summary>
    /// <param name="job">The job's handle.</param>
    /// <param name="informationClass">The class of information set, <see cref="JobObjectExtendedLimitInformationClass"/>.</param>
    /// <param name="information">The limits.</param>
    /// <param name="length">The limits' size, in bytes.</param>
    /// <returns>Whether the limits were set.</returns>
    [LibraryImport("kernel32.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool SetInformationJobObject(nint job, int informationClass, ref JobObjectExtendedLimitInformation information, uint length);

    /// <summary>Puts the process in the job, with every process it starts after.</summary>
    /// <param name="job">The job's handle.</param>
    /// <param name="process">The process's handle.</param>
    /// <returns>Whether the process is in the job.</returns>
    [LibraryImport("kernel32.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool AssignProcessToJobObject(nint job, nint process);

    /// <summary>Closes a handle.</summary>
    /// <param name="handle">The handle.</param>
    /// <returns>Whether it closed.</returns>
    [LibraryImport("kernel32.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool CloseHandle(nint handle);

    /// <summary>A job's basic limits, as <c>JOBOBJECT_BASIC_LIMIT_INFORMATION</c> lays them out.</summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct JobObjectBasicLimitInformation
    {
        public long PerProcessUserTimeLimit;
        public long PerJobUserTimeLimit;
        public uint LimitFlags;
        public nuint MinimumWorkingSetSize;
        public nuint MaximumWorkingSetSize;
        public uint ActiveProcessLimit;
        public nuint Affinity;
        public uint PriorityClass;
        public uint SchedulingClass;
    }

    /// <summary>A job's input and output counts, as <c>IO_COUNTERS</c> lays them out.</summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct IoCounters
    {
        public ulong ReadOperationCount;
        public ulong WriteOperationCount;
        public ulong OtherOperationCount;
        public ulong ReadTransferCount;
        public ulong WriteTransferCount;
        public ulong OtherTransferCount;
    }

    /// <summary>A job's extended limits, as <c>JOBOBJECT_EXTENDED_LIMIT_INFORMATION</c> lays them out.</summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct JobObjectExtendedLimitInformation
    {
        public JobObjectBasicLimitInformation BasicLimitInformation;
        public IoCounters IoInfo;
        public nuint ProcessMemoryLimit;
        public nuint JobMemoryLimit;
        public nuint PeakProcessMemoryUsed;
        public nuint PeakJobMemoryUsed;
    }
}
