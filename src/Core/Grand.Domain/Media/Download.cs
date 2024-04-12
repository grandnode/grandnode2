namespace Grand.Domain.Media;

/// <summary>
///     Represents a download
/// </summary>
public class Download : BaseEntity
{
    /// <summary>
    ///     Gets or sets a customer identifier
    /// </summary>
    public string CustomerId { get; set; }

    /// <summary>
    ///     Gets or sets a GUID
    /// </summary>
    public Guid DownloadGuid { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether DownloadUrl property should be used
    /// </summary>
    public bool UseDownloadUrl { get; set; }

    /// <summary>
    ///     Gets or sets a download URL
    /// </summary>
    public string DownloadUrl { get; set; }

    /// <summary>
    ///     Gets or sets the download binary
    /// </summary>
    public byte[] DownloadBinary { get; set; }

    /// <summary>
    ///     Gets or sets the download binary
    /// </summary>
    public string DownloadObjectId { get; set; }

    /// <summary>
    ///     The mime-type of the download
    /// </summary>
    public string ContentType { get; set; }

    /// <summary>
    ///     The filename of the download
    /// </summary>
    public string Filename { get; set; }

    /// <summary>
    ///     Gets or sets the extension
    /// </summary>
    public string Extension { get; set; }

    /// <summary>
    ///     Gets or sets an download type
    /// </summary>
    public DownloadType DownloadType { get; set; }

    /// <summary>
    ///     Gets or sets an object reference identifier
    /// </summary>
    public string ReferenceId { get; set; }
}