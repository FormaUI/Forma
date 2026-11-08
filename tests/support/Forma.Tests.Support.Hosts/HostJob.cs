namespace Forma.Tests.Hosts;

using System.Diagnostics;
using System.Runtime.InteropServices;

/// <summary>The job every host a check starts runs in on Windows, which ends each of them, with every process it started, as the check's own process ends, however it ends.</summary>
/// <remarks>
/// A check stops the hosts it started as it ends; the job is what stops them where the check's process ends first, a run stopped from
/// its console or its editor among the ways, so no host, its WebView2 browser or its temporary data outlives the run unseen. A host the
/// job can't take is left to the check's own stopping. Elsewhere a host shares the console of the check's process, so a stop from that
/// console reaches it.
/// </remarks>
internal static class HostJob
{
    private static readonly Lazy<nint> Job = new(CreateJob);

    /// <summary>Puts <paramref name="process"/> in the job, where the machine runs Windows.</summary>
    /// <param name="process">The host's process, started.</param>
    public static void Add(Process process)
    {
        if (OperatingSystem.IsWindows() && Job.Value != 0)
        {
            _ = NativeMethods.AssignProcessToJobObject(Job.Value, process.Handle);
        }
    }

    // The job's one handle stays open for the run, so the job ends its processes once the run's process ends and the system closes it.
    private static nint CreateJob()
    {
        var job = NativeMethods.CreateJobObject(0, null);

        if (job == 0)
        {
            return 0;
        }

        var limits = new NativeMethods.JobObjectExtendedLimitInformation
        {
            BasicLimitInformation = new() { LimitFlags = NativeMethods.JobObjectLimitKillOnJobClose },
        };

        if (NativeMethods.SetInformationJobObject(job, NativeMethods.JobObjectExtendedLimitInformationClass, ref limits, (uint)Marshal.SizeOf<NativeMethods.JobObjectExtendedLimitInformation>()))
        {
            return job;
        }

        _ = NativeMethods.CloseHandle(job);

        return 0;
    }
}
