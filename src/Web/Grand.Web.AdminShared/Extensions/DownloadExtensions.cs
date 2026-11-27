using Grand.Domain.Media;
using System.Diagnostics.CodeAnalysis;

namespace Grand.Web.AdminShared.Extensions;

public static class DownloadExtensions
{
    //a notes form uploads with its record's download type and the record id as the reference
    public static bool IsAttachmentOf([NotNullWhen(true)] this Download? download, DownloadType downloadType,
        string referenceId) =>
        download != null && download.DownloadType == downloadType && download.ReferenceId == referenceId;
}
