namespace Forma.Tests.Hosts;

using System.Diagnostics;
using System.Net;

/// <summary>A published web host running in a process of its own, on a free port of this machine.</summary>
public sealed class WebHostProcess : IAsyncDisposable
{
    private readonly Process _process;

    private WebHostProcess(Process process, Uri address)
    {
        _process = process;
        Address = address;
    }

    /// <summary>The address the host listens on.</summary>
    public Uri Address { get; }

    /// <summary>Starts the host <paramref name="assembly"/> names from its publish in <paramref name="folder"/>, in production, once the address <paramref name="readyPath"/> names answers.</summary>
    /// <param name="folder">The folder the host was published to.</param>
    /// <param name="assembly">The host's assembly, its file name.</param>
    /// <param name="readyPath">The path, relative to the host's address, that answers once the host is ready.</param>
    /// <param name="environment">The environment variables the host starts with beside its own, by name.</param>
    /// <returns>The host, answering.</returns>
    public static async Task<WebHostProcess> StartAsync(string folder, string assembly, string readyPath, IReadOnlyDictionary<string, string?>? environment = null)
    {
        var address = new Uri($"http://127.0.0.1:{LocalPorts.GetFree()}/");
        var start = new ProcessStartInfo("dotnet", assembly)
        {
            WorkingDirectory = folder,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        start.Environment["ASPNETCORE_URLS"] = address.ToString().TrimEnd('/');
        start.Environment["ASPNETCORE_ENVIRONMENT"] = "Production";

        foreach (var (name, value) in environment ?? new Dictionary<string, string?>())
        {
            start.Environment[name] = value;
        }

        var process = Process.Start(start) ?? throw new InvalidOperationException($"{assembly} did not start.");
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        try
        {
            await WaitForReadyAsync(process, new Uri(address, readyPath)).ConfigureAwait(false);
        }
        catch
        {
            await StopAsync(process).ConfigureAwait(false);

            throw;
        }

        return new WebHostProcess(process, address);
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
        => StopAsync(_process);

    private static async ValueTask StopAsync(Process process)
    {
        if (!process.HasExited)
        {
            process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync().ConfigureAwait(false);
        }

        process.Dispose();
    }

    private static async Task WaitForReadyAsync(Process process, Uri ready)
    {
        using var client = new HttpClient();
        var deadline = DateTime.UtcNow.AddMinutes(1);

        while (DateTime.UtcNow < deadline)
        {
            if (process.HasExited)
            {
                throw new InvalidOperationException($"The host exited with code {process.ExitCode} before it answered.");
            }

            try
            {
                using var response = await client.GetAsync(ready).ConfigureAwait(false);

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

        throw new TimeoutException($"The host at {ready} did not answer within a minute.");
    }
}
