namespace Forma.Tests.Hosts;

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;

/// <summary>A host a check started in a process of its own, in the job of the check's process, with what it writes recorded a line apiece as it writes it.</summary>
internal sealed class HostProcess : IAsyncDisposable
{
    private static readonly TimeSpan AnswerTimeout = TimeSpan.FromMinutes(1);

    private readonly Process _process;
    private readonly ConcurrentQueue<string> _output = new();

    private HostProcess(Process process, string name)
    {
        _process = process;
        Name = name;
    }

    /// <summary>The host's name, which a failure names it by.</summary>
    public string Name { get; }

    /// <summary>The host's process.</summary>
    public int Id
        => _process.Id;

    /// <summary>Whether the host's process has exited.</summary>
    public bool HasExited
        => _process.HasExited;

    /// <summary>Starts the host <paramref name="start"/> describes, reading what it writes, its output and its errors.</summary>
    /// <param name="start">How the host starts; its output and its errors are redirected here.</param>
    /// <param name="name">The host's name, which a failure names it by.</param>
    /// <returns>The host, started.</returns>
    /// <exception cref="InvalidOperationException">The host didn't start.</exception>
    public static HostProcess Start(ProcessStartInfo start, string name)
    {
        start.UseShellExecute = false;
        start.RedirectStandardOutput = true;
        start.RedirectStandardError = true;

        var host = new HostProcess(new Process { StartInfo = start }, name);
        host._process.OutputDataReceived += (_, args) => host.Record(args.Data);
        host._process.ErrorDataReceived += (_, args) => host.Record(args.Data);

        if (!host._process.Start())
        {
            host._process.Dispose();

            throw new InvalidOperationException($"{name} did not start.");
        }

        HostJob.Add(host._process);
        host._process.BeginOutputReadLine();
        host._process.BeginErrorReadLine();

        return host;
    }

    /// <summary>The last of what the host wrote, a line apiece.</summary>
    /// <param name="count">How many lines to give at most.</param>
    /// <returns>The lines, the newest last.</returns>
    public string GetOutput(int count)
        => string.Join(Environment.NewLine, _output.TakeLast(count));

    /// <summary>What the host wrote, all of it, as a sentence a failure ends with.</summary>
    /// <returns>The sentence, with a space ahead of it.</returns>
    public string DescribeOutput()
        => _output.IsEmpty ? " It wrote nothing." : $" It wrote:{Environment.NewLine}{string.Join(Environment.NewLine, _output)}";

    /// <summary>Waits a minute at most for <paramref name="address"/> to answer with success, asking again until it does.</summary>
    /// <param name="address">The address the host answers once it is ready.</param>
    /// <returns>A task that completes once the address answered.</returns>
    /// <exception cref="InvalidOperationException">The host exited before the address answered.</exception>
    /// <exception cref="TimeoutException">The address didn't answer within a minute.</exception>
    public async Task WaitForAnswerAsync(Uri address)
    {
        using var client = new HttpClient();
        var deadline = DateTime.UtcNow + AnswerTimeout;

        while (DateTime.UtcNow < deadline)
        {
            if (_process.HasExited)
            {
                throw new InvalidOperationException($"{Name} exited with code {_process.ExitCode} before {address} answered.{DescribeOutput()}");
            }

            try
            {
                using var response = await client.GetAsync(address).ConfigureAwait(false);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    return;
                }
            }
            catch (HttpRequestException)
            {
                // Not listening yet; the next attempt asks again.
            }

            await Task.Delay(TimeSpan.FromMilliseconds(250)).ConfigureAwait(false);
        }

        throw new TimeoutException($"{Name} didn't answer at {address} within a minute.{DescribeOutput()}");
    }

    /// <summary>Waits for the host's process to exit, as it does once it is done.</summary>
    /// <param name="timeout">How long to wait at most.</param>
    /// <returns>A task that completes once the process exited.</returns>
    public Task WaitForExitAsync(TimeSpan timeout)
        => _process.WaitForExitAsync().WaitAsync(timeout);

    /// <summary>Stops the host, with every process it started, where it still runs.</summary>
    /// <returns>A task that completes once the host's process exited.</returns>
    public async ValueTask DisposeAsync()
    {
        if (!_process.HasExited)
        {
            _process.Kill(entireProcessTree: true);
            await _process.WaitForExitAsync().ConfigureAwait(false);
        }

        _process.Dispose();
    }

    private void Record(string? line)
    {
        if (line is not null)
        {
            _output.Enqueue(line);
        }
    }
}
