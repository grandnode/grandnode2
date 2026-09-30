using System.Net;
using System.Net.Sockets;
using Grand.Business.Core.Utilities.System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Business.Catalog.Tests.Services.ExportImport;

[TestClass]
public class DownloadUrlTests
{
    [TestMethod]
    [DataRow("127.0.0.1")]
    [DataRow("0.0.0.0")]
    [DataRow("10.1.2.3")]
    [DataRow("100.64.0.1")]
    [DataRow("169.254.169.254")]
    [DataRow("172.16.0.1")]
    [DataRow("172.31.255.255")]
    [DataRow("192.168.1.1")]
    [DataRow("::")]
    [DataRow("::1")]
    [DataRow("fe80::1")]
    [DataRow("fc00::1")]
    [DataRow("fd12:3456::1")]
    [DataRow("ff02::1")]
    [DataRow("::ffff:127.0.0.1")]
    [DataRow("::ffff:169.254.169.254")]
    [DataRow("64:ff9b::a9fe:a9fe")]
    [DataRow("2002:7f00:1::1")]
    [DataRow("2001:0:4136:e378::1")]
    public void IsPublicAddress_NonPublicAddress_ReturnsFalse(string address)
    {
        Assert.IsFalse(DownloadUrl.IsPublicAddress(IPAddress.Parse(address)));
    }

    [TestMethod]
    [DataRow("8.8.8.8")]
    [DataRow("1.1.1.1")]
    [DataRow("172.32.0.1")]
    [DataRow("::ffff:8.8.8.8")]
    [DataRow("2606:4700:4700::1111")]
    public void IsPublicAddress_PublicAddress_ReturnsTrue(string address)
    {
        Assert.IsTrue(DownloadUrl.IsPublicAddress(IPAddress.Parse(address)));
    }

    [TestMethod]
    [DataRow("http://127.0.0.1/image.png")]
    [DataRow("http://169.254.169.254/latest/meta-data/")]
    [DataRow("http://10.0.0.5/image.png")]
    [DataRow("http://192.168.1.5/image.png")]
    [DataRow("http://[::1]/image.png")]
    [DataRow("http://[::ffff:127.0.0.1]/image.png")]
    [DataRow("http://0x7f.1/image.png")]
    [DataRow("http://localhost/image.png")]
    [DataRow("http://example.com:8080/image.png")]
    [DataRow("http://nonexistent.invalid/image.png")]
    public async Task DownloadImage_DisallowedTarget_ReturnsNull(string url)
    {
        Assert.IsNull(await DownloadUrl.DownloadImage(url));
    }

    [TestMethod]
    [DataRow("file:///etc/passwd")]
    [DataRow("ftp://example.com/image.png")]
    [DataRow("/etc/passwd")]
    [DataRow(@"C:\Windows\win.ini")]
    [DataRow(@"\\server\share\image.png")]
    [DataRow("not a url")]
    public async Task DownloadImage_NotHttpUrl_ReturnsNull(string url)
    {
        Assert.IsNull(await DownloadUrl.DownloadImage(url));
    }

    [TestMethod]
    [DataRow(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00 }, "image/jpeg")]
    [DataRow(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00 }, "image/png")]
    [DataRow(new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61, 0x01 }, "image/gif")]
    [DataRow(new byte[] { 0x52, 0x49, 0x46, 0x46, 0x00, 0x00, 0x00, 0x00, 0x57, 0x45, 0x42, 0x50 }, "image/webp")]
    public void DetectImageMimeType_SupportedImage_ReturnsMimeType(byte[] binary, string expected)
    {
        Assert.AreEqual(expected, DownloadUrl.DetectImageMimeType(binary));
    }

    [TestMethod]
    [DataRow("{\"AccessKeyId\":\"secret\"}")]
    [DataRow("<html><body>internal</body></html>")]
    [DataRow("<svg xmlns=\"http://www.w3.org/2000/svg\"><script>alert(1)</script></svg>")]
    [DataRow("")]
    public void DetectImageMimeType_NotAnImage_ReturnsNull(string content)
    {
        Assert.IsNull(DownloadUrl.DetectImageMimeType(Encoding.UTF8.GetBytes(content)));
    }

    [TestMethod]
    [DataRow("127.0.0.1", "127.0.0.1")]
    [DataRow("localhost", "LOCALHOST")]
    public async Task DownloadImage_AllowedPrivateHost_DownloadsFromInternalServer(string host, string allowedHost)
    {
        using var server = new LocalImageServer();

        var result = await DownloadUrl.DownloadImage($"http://{host}:{server.Port}/image.png", [allowedHost]);

        Assert.IsNotNull(result);
        Assert.AreEqual("image/png", result.MimeType);
        CollectionAssert.AreEqual(LocalImageServer.Png, result.Binary);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow(new[] { "images.example.local", "*.example.local", " " })]
    public async Task DownloadImage_InternalServerNotAllowed_NeverConnects(string[] allowedHosts)
    {
        using var server = new LocalImageServer();

        var result = await DownloadUrl.DownloadImage($"http://127.0.0.1:{server.Port}/image.png", allowedHosts);

        Assert.IsNull(result);
        Assert.AreEqual(0, server.Connections);
    }

    private sealed class LocalImageServer : IDisposable
    {
        public static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x01, 0x02, 0x03];

        private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
        private int _connections;

        public LocalImageServer()
        {
            _listener.Start();
            _ = Serve();
        }

        public int Port => ((IPEndPoint)_listener.LocalEndpoint).Port;
        public int Connections => _connections;

        private async Task Serve()
        {
            try
            {
                while (true)
                {
                    using var client = await _listener.AcceptTcpClientAsync();
                    Interlocked.Increment(ref _connections);
                    var stream = client.GetStream();
                    await stream.ReadAsync(new byte[4096]);
                    var header = "HTTP/1.1 200 OK\r\nContent-Type: image/png\r\n" +
                                 $"Content-Length: {Png.Length}\r\nConnection: close\r\n\r\n";
                    await stream.WriteAsync(Encoding.ASCII.GetBytes(header));
                    await stream.WriteAsync(Png);
                }
            }
            catch (Exception ex) when (ex is SocketException or ObjectDisposedException)
            {
                //listener stopped
            }
        }

        public void Dispose()
        {
            _listener.Stop();
        }
    }
}
