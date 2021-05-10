using Grand.Business.Storage.Interfaces;
using Grand.Business.Storage.Services;
using Grand.Infrastructure;
using Grand.Infrastructure.Configuration;
using Grand.SharedKernel.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Grand.Business.Storage.Startup
{
    public class StartupApplication : IStartupApplication
    {
        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            var config = new AppConfig();
            configuration.GetSection("Application").Bind(config);

            services.AddScoped<IDownloadService, DownloadService>();

            //picture service
            var useAzureBlobStorage = !String.IsNullOrEmpty(config.AzureBlobStorageConnectionString);
            var useAmazonBlobStorage = (!String.IsNullOrEmpty(config.AmazonAwsAccessKeyId) && !String.IsNullOrEmpty(config.AmazonAwsSecretAccessKey) && !String.IsNullOrEmpty(config.AmazonBucketName) && !String.IsNullOrEmpty(config.AmazonRegion));

            if (useAzureBlobStorage)
            {
                //Windows Azure BLOB
                services.AddScoped<IPictureService, AzurePictureService>();
            }
            else if (useAmazonBlobStorage)
            {
                //Amazon S3 Simple Storage Service
                services.AddScoped<IPictureService, AmazonPictureService>();
            }
            else
            {
                //standard file system
                services.AddScoped<IPictureService, PictureService>();
            }

            services.AddSingleton<IMediaFileStore>(serviceProvider =>
            {
                var fileStore = new FileSystemStore(CommonPath.WebRootPath);
                return new DefaultMediaFileStore(fileStore);
            });

        }
        public void Configure(IApplicationBuilder application, IWebHostEnvironment webHostEnvironment)
        {

        }
        public int Priority => 100;
        public bool BeforeConfigure => false;
    }
}
