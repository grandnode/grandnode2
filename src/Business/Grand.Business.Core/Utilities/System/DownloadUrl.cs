using System.Net;
using System.Net.Sockets;

namespace Grand.Business.Core.Utilities.System;

/// <summary>
///     Downloads images referenced by URL in import files. The URL comes from whoever prepared the file,
///     so the server must not become a proxy into its own network (SSRF): only http(s) on ports 80/443 to
///     publicly routable addresses is allowed, and only content that is actually an image is returned.
/// </summary>
public static class DownloadUrl
{
    private const int MaxResponseBytes = 10 * 1024 * 1024;
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(30);

    private static readonly IPNetwork[] BlockedIPv4Networks = [
        IPNetwork.Parse("0.0.0.0/8"), //0.0.0.0 reaches localhost on Linux
        IPNetwork.Parse("127.0.0.0/8"), //loopback
        IPNetwork.Parse("10.0.0.0/8"), //private network
        IPNetwork.Parse("172.16.0.0/12"), //private network
        IPNetwork.Parse("192.168.0.0/16"), //private network
        IPNetwork.Parse("169.254.0.0/16"), //link-local, cloud metadata endpoints
        IPNetwork.Parse("100.64.0.0/10") //carrier-grade NAT, used as an internal network by some clouds
    ];

    //only global unicast is public; within it 6to4 and Teredo embed an IPv4 address that could be private
    private static readonly IPNetwork IPv6GlobalUnicast = IPNetwork.Parse("2000::/3");

    private static readonly IPNetwork[] BlockedIPv6Networks = [
        IPNetwork.Parse("2001::/32"), //Teredo
        IPNetwork.Parse("2002::/16") //6to4
    ];

    //the address is validated when the socket is opened, not before the request, so a DNS answer
    //that changes between the check and the connect (DNS rebinding) cannot redirect the request;
    //redirects open their connections through the same callback
    private static readonly HttpClient Client = new(new SocketsHttpHandler {
        UseProxy = false,
        AllowAutoRedirect = true,
        MaxAutomaticRedirections = 3,
        ConnectTimeout = TimeSpan.FromSeconds(10),
        PooledConnectionLifetime = TimeSpan.FromMinutes(2),
        ConnectCallback = ConnectToPublicAddress
    }) {
        Timeout = Timeout.InfiniteTimeSpan
    };

    /// <summary>
    ///     Downloads an image from a public http(s) URL
    /// </summary>
    /// <param name="url">Image URL</param>
    /// <returns>Image binary with the MIME type detected from its content, or null when the URL is not allowed,
    /// the download fails or the content is not a supported image</returns>
    public static async Task<DownloadedImage> DownloadImage(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            return null;

        using var cts = new CancellationTokenSource(RequestTimeout);
        try
        {
            using var response = await Client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cts.Token);
            if (!response.IsSuccessStatusCode)
                return null;

            if (response.Content.Headers.ContentLength > MaxResponseBytes)
                return null;

            var binary = await ReadLimited(response.Content, cts.Token);
            if (binary == null)
                return null;

            var mimeType = DetectImageMimeType(binary);
            return mimeType == null ? null : new DownloadedImage(binary, mimeType);
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or IOException)
        {
            return null;
        }
    }

    /// <summary>
    ///     Returns the MIME type of a JPEG, PNG, GIF or WebP image based on its signature, or null for any other content
    /// </summary>
    public static string DetectImageMimeType(ReadOnlySpan<byte> binary)
    {
        if (binary.StartsWith((ReadOnlySpan<byte>)[0xFF, 0xD8, 0xFF]))
            return "image/jpeg";
        if (binary.StartsWith((ReadOnlySpan<byte>)[0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]))
            return "image/png";
        if (binary.StartsWith("GIF87a"u8) || binary.StartsWith("GIF89a"u8))
            return "image/gif";
        if (binary.Length >= 12 && binary.StartsWith("RIFF"u8) && binary.Slice(8, 4).SequenceEqual("WEBP"u8))
            return "image/webp";
        return null;
    }

    /// <summary>
    ///     Returns true when the address is publicly routable (not loopback, private, link-local, reserved etc.)
    /// </summary>
    public static bool IsPublicAddress(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6)
            address = address.MapToIPv4();

        return address.AddressFamily switch {
            AddressFamily.InterNetwork => !BlockedIPv4Networks.Any(n => n.Contains(address)),
            AddressFamily.InterNetworkV6 => IPv6GlobalUnicast.Contains(address) &&
                                            !BlockedIPv6Networks.Any(n => n.Contains(address)),
            _ => false
        };
    }

    private static async ValueTask<Stream> ConnectToPublicAddress(SocketsHttpConnectionContext context,
        CancellationToken cancellationToken)
    {
        var endpoint = context.DnsEndPoint;
        if (endpoint.Port is not (80 or 443))
            throw new HttpRequestException($"Port {endpoint.Port} is not allowed");

        IPAddress[] addresses = IPAddress.TryParse(endpoint.Host, out var literal)
            ? [literal]
            : await Dns.GetHostAddressesAsync(endpoint.Host, cancellationToken);

        //reject when any address is non-public, so a public record cannot smuggle in a private one
        if (addresses.Length == 0 || !addresses.All(IsPublicAddress))
            throw new HttpRequestException($"Host {endpoint.Host} is not allowed");

        var socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
        try
        {
            await socket.ConnectAsync(addresses, endpoint.Port, cancellationToken);
            return new NetworkStream(socket, true);
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }

    private static async Task<byte[]> ReadLimited(HttpContent content, CancellationToken cancellationToken)
    {
        await using var stream = await content.ReadAsStreamAsync(cancellationToken);
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        int read;
        while ((read = await stream.ReadAsync(chunk, cancellationToken)) > 0)
        {
            if (buffer.Length + read > MaxResponseBytes)
                return null;
            buffer.Write(chunk, 0, read);
        }

        return buffer.ToArray();
    }
}

public record DownloadedImage(byte[] Binary, string MimeType);
