namespace Forma.Tests.Hosts;

using System.Net;
using System.Net.Sockets;

/// <summary>The ports of this machine a host started for a check listens on.</summary>
public static class LocalPorts
{
    /// <summary>A port nothing on this machine listens on now.</summary>
    /// <returns>The port.</returns>
    public static int GetFree()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();

        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }
}
