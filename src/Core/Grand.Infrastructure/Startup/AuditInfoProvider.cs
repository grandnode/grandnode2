using Grand.Data;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Grand.Infrastructure.Startup;

public class AuditInfoProvider : IAuditInfoProvider
{
    private readonly IHttpContextAccessor _contextAccessor;

    public AuditInfoProvider(IHttpContextAccessor contextAccessor)
    {
        _contextAccessor = contextAccessor;
    }

    public string GetCurrentUser()
    {
        return _contextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Name)?.Value;
    }

    public DateTime GetCurrentDateTime()
    {
        return DateTime.UtcNow;
    }
}