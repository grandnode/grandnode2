using Grand.Business.Marketing.Interfaces.Banners;
using Grand.Business.Marketing.Interfaces.Campaigns;
using Grand.Business.Marketing.Interfaces.Contacts;
using Grand.Business.Marketing.Interfaces.Courses;
using Grand.Business.Marketing.Interfaces.Customers;
using Grand.Business.Marketing.Interfaces.Documents;
using Grand.Business.Marketing.Interfaces.Knowledgebase;
using Grand.Business.Marketing.Interfaces.Newsletters;
using Grand.Business.Marketing.Interfaces.PushNotifications;
using Grand.Business.Marketing.Services.Banners;
using Grand.Business.Marketing.Services.Campaigns;
using Grand.Business.Marketing.Services.Contacts;
using Grand.Business.Marketing.Services.Courses;
using Grand.Business.Marketing.Services.Customers;
using Grand.Business.Marketing.Services.Documents;
using Grand.Business.Marketing.Services.Knowledgebase;
using Grand.Business.Marketing.Services.Newsteletters;
using Grand.Business.Marketing.Services.PushNotifications;
using Grand.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Grand.Business.Marketing.Startup
{
    public class StartupApplication : IStartupApplication
    {
        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            RegisterCoursesService(services);
            RegisterDocumentsService(services);
            RegisterKnowledgebaseService(services);
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
            serviceCollection.AddScoped<ICustomerActionService, CustomerActionService>();
            serviceCollection.AddScoped<ICustomerActionEventService, CustomerActionEventService>();
            serviceCollection.AddScoped<ICustomerReminderService, CustomerReminderService>();
            serviceCollection.AddScoped<ICustomerProductService, CustomerProductService>();
            serviceCollection.AddScoped<ICustomerCoordinatesService, CustomerCoordinatesService>();
        }
        private void RegisterCommon(IServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped<IBannerService, BannerService>();
            serviceCollection.AddScoped<IPopupService, PopupService>();
            serviceCollection.AddScoped<IInteractiveFormService, InteractiveFormService>();
            serviceCollection.AddScoped<IPushNotificationsService, PushNotificationsService>();
            serviceCollection.AddScoped<IContactAttributeParser, ContactAttributeParser>();
            serviceCollection.AddScoped<IContactAttributeService, ContactAttributeService>();
            serviceCollection.AddScoped<IContactUsService, ContactUsService>();
            serviceCollection.AddScoped<INewsLetterSubscriptionService, NewsLetterSubscriptionService>();
            serviceCollection.AddScoped<INewsletterCategoryService, NewsletterCategoryService>();
            serviceCollection.AddScoped<ICampaignService, CampaignService>();
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
        private void RegisterKnowledgebaseService(IServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped<IKnowledgebaseService, KnowledgebaseService>();
        }

    }
}
