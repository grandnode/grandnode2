namespace Grand.Business.Core.Enums.Messages
{
    public enum SecureSocketOptions
    {
        //
        // Summary:
        //     No SSL or TLS encryption should be used.
        None = 0,
        //
        // Summary:
        //     Allow the MailKit.IMailService to decide which SSL or TLS options to use (default).
        //     If the server does not support SSL or TLS, then the connection will continue
        //     without any encryption.
        Auto = 1,
        //
        // Summary:
        //     The connection should use SSL or TLS encryption immediately.
        SslOnConnect = 2,
        //
        // Summary:
        //     Elevates the connection to use TLS encryption immediately after reading the greeting
        //     and capabilities of the server. If the server does not support the STARTTLS extension,
        //     then the connection will fail and a System.NotSupportedException will be thrown.
        StartTls = 3,
        //
        // Summary:
        //     Elevates the connection to use TLS encryption immediately after reading the greeting
        //     and capabilities of the server, but only if the server supports the STARTTLS
        //     extension.
        StartTlsWhenAvailable = 4

    }
}
