using Grand.Infrastructure;
using Grand.Infrastructure.Configuration;
using Grand.Module.Api.ApiExplorer;
using Grand.Module.Api.Attributes;
using Grand.Module.Api.Infrastructure.Extensions;
using Grand.Module.Api.Infrastructure.Transformers;
using Grand.SharedKernel.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Scalar.AspNetCore;

namespace Grand.Module.Api.Infrastructure;

public class OpenApiStartup : IStartupApplication
{
    public void Configure(WebApplication application, IWebHostEnvironment webHostEnvironment)
    {
        application.MapOpenApi();
        if (application.Environment.IsDevelopment())
        {
            application.MapScalarApiReference(options =>
            {
                options.WithTitle("OpenApi Playground");
                options.WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);   
            });
        }
    }

    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        var backendApiConfig = services.BuildServiceProvider().GetService<BackendAPIConfig>();
        var frontApiConfig = services.BuildServiceProvider().GetService<FrontendAPIConfig>();
        var webHostEnvironment = services.BuildServiceProvider().GetService<IWebHostEnvironment>();

        if (webHostEnvironment.IsDevelopment() && (backendApiConfig.Enabled || frontApiConfig.Enabled))
        {
            if (backendApiConfig.Enabled)
            {
                ConfigureBackendApi(services);
            }
            if (frontApiConfig.Enabled)
            {
                ConfigureFrontendApi(services);
            }
        }

        if (backendApiConfig.Enabled)
        {
            //register RequestHandler
            services.RegisterRequestHandler();

            //Add JsonPatchInputFormatter
            services.AddControllers(options =>
            {
                options.InputFormatters.Insert(0, services.GetJsonPatchInputFormatter());
            });
            services.AddScoped<ModelValidationAttribute>();
        }
    }
    public int Priority => 505;
    public bool BeforeConfigure => false;

    private void ConfigureBackendApi(IServiceCollection services)
    {
        services.AddOpenApi(ApiConstants.ApiGroupNameV1, options =>
        {
            options.OpenApiVersion = Microsoft.OpenApi.OpenApiSpecVersion.OpenApi3_0;
            options.AddContactDocumentTransformer("Grandnode Backend API", ApiConstants.ApiGroupNameV1);
            options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
            options.AddSchemaTransformer<EnumSchemaTransformer>();
            options.AddOperationTransformer();
            options.AddClearServerDocumentTransformer();
        });
    }

    private void ConfigureFrontendApi(IServiceCollection services)
    {
        services.AddOpenApi(ApiConstants.ApiGroupNameV2, options =>
        {
            options.OpenApiVersion = Microsoft.OpenApi.OpenApiSpecVersion.OpenApi3_0;
            options.AddContactDocumentTransformer("Grandnode Frontend API", ApiConstants.ApiGroupNameV2);
            options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
            options.AddSchemaTransformer<EnumSchemaTransformer>();
            options.AddSchemaTransformer<IgnoreFieldSchemaTransformer>();
            options.AddCsrfTokenTransformer();
            options.AddClearServerDocumentTransformer();
        });

        //api description provider
        services.TryAddEnumerable(ServiceDescriptor.Transient<IApiDescriptionProvider, MetadataApiDescriptionProvider>());
    }
}
