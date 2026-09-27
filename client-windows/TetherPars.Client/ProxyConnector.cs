using System.Diagnostics;
using System.Net.Sockets;
using System.Text;

namespace TetherPars.Client;

/// <summary>
/// Task 4: Windows connector in manual-proxy mode.
/// After `adb forward tcp:8000 tcp:8000`, the phone proxy (Task 3)
/// is reachable at 127.0.0.1:8000. This class verifies the road
/// by sending a real `CONNECT` and expecting `200`.
/// </summary>
public class ProxyConnector
{
    public string Host { get; }
    public int Port { get; }
    public int TimeoutMs { get; }

    public ProxyConnector(string host = "127.0.0.1", int port = 8000, int timeoutMs = 15000)
    {
        Host = host;
        Port = port;
        TimeoutMs = timeoutMs;
    }

    /// <summary>
    /// Runs `adb forward tcp:8000 tcp:8000` so Windows 127.0.0.1:8000
    /// maps to the phone's LocalProxyServer. Returns true when adb exits 0.
    /// </summary>
    public static bool EnsureAdbForward(int localPort = 8000, int remotePort = 8000)
    {
        try
        {
            using var p = Process.Start(new ProcessStartInfo
            {
                FileName = "adb",
                Arguments = $"forward tcp:{localPort} tcp:{remotePort}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            });
            if (p is null) return false;
            p.WaitForExit(15000);
            return p.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Sends `CONNECT example.com:80` to the proxy and expects `200`.
    /// This is the same assertion as LocalProxyServerTest on Android.
    /// </summary>
    public bool TestViaAdbForward(string probeHost = "example.com", int probePort = 80)
    {
        try
        {
            using var client = new TcpClient();
            var connectTask = client.ConnectAsync(Host, Port);
            if (!connectTask.Wait(TimeoutMs)) return false;

            using var stream = client.GetStream();
            stream.WriteTimeout = TimeoutMs;
            stream.ReadTimeout = TimeoutMs;

            var req = $"CONNECT {probeHost}:{probePort} HTTP/1.1\r\nHost: {probeHost}:{probePort}\r\n\r\n";
            var reqBytes = Encoding.ASCII.GetBytes(req);
            stream.Write(reqBytes, 0, reqBytes.Length);
            stream.Flush();

            var buf = new byte[1024];
            var readTask = stream.ReadAsync(buf, 0, buf.Length);
            if (!readTask.Wait(TimeoutMs)) return false;
            var n = readTask.Result;
            if (n <= 0) return false;
            var reply = Encoding.ASCII.GetString(buf, 0, n);
            return reply.Contains("200");
        }
        catch
        {
            return false;
        }
    }
}
