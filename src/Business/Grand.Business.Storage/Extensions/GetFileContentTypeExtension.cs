using Microsoft.AspNetCore.StaticFiles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grand.Business.Storage.Extensions
{    
    public static class GetFileContentTypeExtension
    {
        /// <summary>
        /// Get File ContentType based on file extension
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static string GetFileContentType(this string fileName)
        {
            var provider = new FileExtensionContentTypeProvider();
            if (!provider.TryGetContentType(fileName, out var contentType))
            {
                contentType = "application/octet-stream";
            }
            return contentType;
        }
    }
}
