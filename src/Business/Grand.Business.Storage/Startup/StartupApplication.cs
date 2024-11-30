using DotLiquid.Tags;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Business.Storage.Services;
using Grand.Infrastructure;
using Grand.Infrastructure.Configuration;
using Grand.SharedKernel.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Grand.Business.Storage.Startup;

public class StartupApplication : IStartupApplication
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IDownloadService, DownloadService>();

        //picture service
        var azureConfig = new AzureConfig();
        configuration.GetSection("Azure").Bind(azureConfig);
        var useAzureBlobStorage = !string.IsNullOrEmpty(azureConfig.AzureBlobStorageConnectionString);

        var amazonConfig = new AmazonConfig();
        configuration.GetSection("Amazon").Bind(amazonConfig);
        var useAmazonBlobStorage = !string.IsNullOrEmpty(amazonConfig.AmazonAwsAccessKeyId) &&
                                   !string.IsNullOrEmpty(amazonConfig.AmazonAwsSecretAccessKey) &&
                                   !string.IsNullOrEmpty(amazonConfig.AmazonBucketName) &&
                                   !string.IsNullOrEmpty(amazonConfig.AmazonRegion);

        if (useAzureBlobStorage)
            //Windows Azure BLOB
            services.AddScoped<IPictureService, AzurePictureService>();
        else if (useAmazonBlobStorage)
            //Amazon S3 Simple Storage Service
            services.AddScoped<IPictureService, AmazonPictureService>();
        else
            //standard file system
            services.AddScoped<IPictureService, PictureService>();

        services.AddScoped<IMediaFileStore>(serviceProvider =>
        {
            var webHostEnvironment = serviceProvider.GetRequiredService<IWebHostEnvironment>();
            var param = configuration[CommonPath.DirectoryParam];
            var fileStore = new FileSystemStore(Path.Combine(webHostEnvironment.WebRootPath, param ?? ""));
            return new DefaultMediaFileStore(fileStore);
        });
    }

    public void Configure(WebApplication application, IWebHostEnvironment webHostEnvironment)
    {
    }

    public int Priority => 100;
    public bool BeforeConfigure => false;
}