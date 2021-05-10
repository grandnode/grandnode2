using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Business.Storage.Extensions
{
    /// <summary>
    /// Extensions
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Gets the download binary array
        /// </summary>
        /// <param name="file">Posted file</param>
        /// <returns>Download binary array</returns>
        public static byte[] GetDownloadBits(this IFormFile file)
        {
            using (var fileStream = file.OpenReadStream())
            using (var ms = new MemoryStream())
            {
                fileStream.CopyTo(ms);
                var fileBytes = ms.ToArray();
                return fileBytes;
            }
        }

        /// <summary>
        /// Gets the picture binary array
        /// </summary>
        /// <param name="file">File</param>
        /// <returns>Picture binary array</returns>
        public static byte[] GetPictureBits(this IFormFile file)
        {
            return GetDownloadBits(file);
        }

        
    }
}
