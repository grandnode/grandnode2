using Grand.Infrastructure.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Grand.Module.Installer.Startup;

public class EndpointProvider : IEndpointProvider
{
    public int Priority => int.MinValue;

    public void RegisterEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
    {
        endpointRouteBuilder.MapControllerRoute("Install", "install", new { controller = "Install", action = "Index" });
    }
}
