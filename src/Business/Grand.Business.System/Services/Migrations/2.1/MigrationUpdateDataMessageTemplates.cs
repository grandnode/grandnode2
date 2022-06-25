using Grand.Business.Core.Interfaces.Common.Logging;
using Grand.Domain.Data;
using Grand.Infrastructure.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Domain.Messages;

namespace Grand.Business.System.Services.Migrations._2._1
{
    public class MigrationUpdateDataMessageTemplates: IMigration
    {
        public int Priority => 0;
        public DbVersion Version => new(2, 1);
        public Guid Identity => new("AFC66A81-E728-44B0-B9E7-045E4C2D86DE");
        public string Name => "Sets new Data Message Templates";

        /// <summary>
        /// Upgrade process
        /// </summary>
        /// <param name="database"></param>
        /// <param name="serviceProvider"></param>
        /// <returns></returns>
        public bool UpgradeProcess(IDatabaseContext database, IServiceProvider serviceProvider)
        {
            var messageRepository = serviceProvider.GetRequiredService<IMessageTemplateService>();
            var emailRepository = serviceProvider.GetRequiredService<IEmailAccountService>();
            
            var logService = serviceProvider.GetRequiredService<ILogger>();
            
            try
            {

                var eaGeneral = emailRepository.GetAllEmailAccounts().Result.FirstOrDefault();
                if (eaGeneral == null)
                    throw new Exception("Default email account cannot be loaded");

                messageRepository.InsertMessageTemplate(new MessageTemplate {
                    Name = "Customer.EmailLoginCode",
                    Subject = "Login to {{Store.Name}}",
                    Body = "<a href=\"{{Store.URL}}\">{{Store.Name}}</a>  <br />\r\n  <br />\r\n  To login to {{Store.Name}} <a href=\"{{Customer.LoginCodeURL}}\">click here</a>.     <br />\r\n  <br />\r\n  {{Store.Name}}",
                    IsActive = true,
                    EmailAccountId = eaGeneral.Id,
                });
            }
            catch (Exception ex)
            {
                logService.InsertLog(Domain.Logging.LogLevel.Error, "UpgradeProcess - Add new Data Message Template", ex.Message).GetAwaiter().GetResult();
            }
            return true;
        }
    }
}