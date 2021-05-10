using Grand.Business.Messages.Interfaces;
using Microsoft.AspNetCore.StaticFiles;

namespace Grand.Business.Messages.Services
{
    public partial class MimeMappingService : IMimeMappingService
    {
        private readonly FileExtensionContentTypeProvider _contentTypeProvider;

        public MimeMappingService(FileExtensionContentTypeProvider contentTypeProvider)
        {
            _contentTypeProvider = contentTypeProvider;
        }

        public string Map(string filename)
        {
            string contentType;
            if (!_contentTypeProvider.TryGetContentType(filename, out contentType))
            {
                contentType = "application/octet-stream";
            }
            return contentType;
        }
    }
}
