namespace Forma.Tests.Hosts;

using System.Diagnostics;

/// <summary>A published web host running in a process of its own, on a free port of this machine.</summary>
public sealed class WebHostProcess : IAsyncDisposable
{
    private readonly HostProcess _process;

    private WebHostProcess(HostProcess process, Uri address)
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
    /// <remarks>A host that exits or doesn't answer fails with what it wrote, and is stopped.</remarks>
    public static async Task<WebHostProcess> StartAsync(string folder, string assembly, string readyPath, IReadOnlyDictionary<string, string?>? environment = null)
    {
        var address = new Uri($"http://127.0.0.1:{LocalPorts.GetFree()}/");
        var start = new ProcessStartInfo("dotnet", assembly) { WorkingDirectory = folder };
        start.Environment["ASPNETCORE_URLS"] = address.ToString().TrimEnd('/');
        start.Environment["ASPNETCORE_ENVIRONMENT"] = "Production";

        foreach (var (name, value) in environment ?? new Dictionary<string, string?>())
        {
            start.Environment[name] = value;
        }

        var process = HostProcess.Start(start, assembly);

        try
        {
            await process.WaitForAnswerAsync(new Uri(address, readyPath)).ConfigureAwait(false);
        }
        catch
        {
            await process.DisposeAsync().ConfigureAwait(false);

            throw;
        }

        return new WebHostProcess(process, address);
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
        => _process.DisposeAsync();
}
