using Grand.Business.Core.Interfaces.Messages;
using Microsoft.AspNetCore.StaticFiles;

namespace Grand.Business.Messages.Services;

public class MimeMappingService : IMimeMappingService
{
    private readonly FileExtensionContentTypeProvider _contentTypeProvider;

    public MimeMappingService(FileExtensionContentTypeProvider contentTypeProvider)
    {
        _contentTypeProvider = contentTypeProvider;
    }

    public string Map(string filename)
    {
        if (!_contentTypeProvider.TryGetContentType(filename, out var contentType))
            contentType = "application/octet-stream";
        return contentType;
    }
}