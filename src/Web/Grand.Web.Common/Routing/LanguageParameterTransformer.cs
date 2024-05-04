using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Grand.Web.Common.Routing;

public class LanguageParameterTransformer : IOutboundParameterTransformer
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public LanguageParameterTransformer(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string TransformOutbound(object value)
    {
        var lang = _httpContextAccessor.HttpContext?.Request.RouteValues["language"];
        return lang != null ? lang.ToString() : value?.ToString();
    }
}