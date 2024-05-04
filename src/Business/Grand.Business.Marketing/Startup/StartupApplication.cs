using Grand.Business.Core.Interfaces.Marketing.Campaigns;
using Grand.Business.Core.Interfaces.Marketing.Contacts;
using Grand.Business.Core.Interfaces.Marketing.Courses;
using Grand.Business.Core.Interfaces.Marketing.Customers;
using Grand.Business.Core.Interfaces.Marketing.Documents;
using Grand.Business.Core.Interfaces.Marketing.Newsletters;
using Grand.Business.Core.Interfaces.Marketing.PushNotifications;
using Grand.Business.Marketing.Services.Campaigns;
using Grand.Business.Marketing.Services.Contacts;
using Grand.Business.Marketing.Services.Courses;
using Grand.Business.Marketing.Services.Customers;
using Grand.Business.Marketing.Services.Documents;
using Grand.Business.Marketing.Services.Newsletters;
using Grand.Business.Marketing.Services.PushNotifications;
using Grand.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Grand.Business.Marketing.Startup;

public class StartupApplication : IStartupApplication
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        RegisterCoursesService(services);
        RegisterDocumentsService(services);
        RegisterCommon(services);
        RegisterCustomer(services);
    }

    public void Configure(IApplicationBuilder application, IWebHostEnvironment webHostEnvironment)
    {
    }

    public int Priority => 100;
    public bool BeforeConfigure => false;

    private void RegisterCustomer(IServiceCollection serviceCollection)
    {
        serviceCollection.AddScoped<ICustomerTagService, CustomerTagService>();
        serviceCollection.AddScoped<ICustomerProductService, CustomerProductService>();
        serviceCollection.AddScoped<ICustomerCoordinatesService, CustomerCoordinatesService>();
    }

    private void RegisterCommon(IServiceCollection serviceCollection)
    {
        serviceCollection.AddScoped<IContactAttributeParser, ContactAttributeParser>();
        serviceCollection.AddScoped<IContactAttributeService, ContactAttributeService>();
        serviceCollection.AddScoped<IContactUsService, ContactUsService>();
        serviceCollection.AddScoped<INewsLetterSubscriptionService, NewsLetterSubscriptionService>();
        serviceCollection.AddScoped<INewsletterCategoryService, NewsletterCategoryService>();
        serviceCollection.AddScoped<ICampaignService, CampaignService>();

        serviceCollection.AddHttpClient<IPushNotificationsService, PushNotificationsService>();
    }

    private void RegisterCoursesService(IServiceCollection serviceCollection)
    {
        serviceCollection.AddScoped<ICourseActionService, CourseActionService>();
        serviceCollection.AddScoped<ICourseLessonService, CourseLessonService>();
        serviceCollection.AddScoped<ICourseLevelService, CourseLevelService>();
        serviceCollection.AddScoped<ICourseService, CourseService>();
        serviceCollection.AddScoped<ICourseSubjectService, CourseSubjectService>();
    }

    private void RegisterDocumentsService(IServiceCollection serviceCollection)
    {
        serviceCollection.AddScoped<IDocumentTypeService, DocumentTypeService>();
        serviceCollection.AddScoped<IDocumentService, DocumentService>();
    }
}