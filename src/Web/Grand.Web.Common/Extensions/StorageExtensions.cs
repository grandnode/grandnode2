﻿using Microsoft.AspNetCore.Http;

namespace Grand.Web.Common.Extensions;

/// <summary>
///     Storage extensions
/// </summary>
public static class Extensions
{
    /// <summary>
    ///     Gets the download binary array
    /// </summary>
    /// <param name="file">Posted file</param>
    /// <returns>Download binary array</returns>
    public static byte[] GetDownloadBits(this IFormFile file)
    {
        using var fileStream = file.OpenReadStream();
        using var ms = new MemoryStream();
        fileStream.CopyTo(ms);
        var fileBytes = ms.ToArray();
        return fileBytes;
    }
}