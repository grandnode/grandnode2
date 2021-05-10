using Grand.Infrastructure;
using Grand.Domain.Logging;
using Grand.Web.Common.Extensions;
using Grand.Business.Customers.Interfaces;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Interfaces.Logging;
using Grand.Web.Admin.Interfaces;
using Grand.Web.Admin.Models.Logging;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Grand.Business.Common.Extensions;

namespace Grand.Web.Admin.Services
{
    public partial class LogViewModelService: ILogViewModelService
    {
        private readonly ILogger _logger;
        private readonly IWorkContext _workContext;
        private readonly ITranslationService _translationService;
        private readonly IDateTimeService _dateTimeService;
        private readonly IServiceProvider _serviceProvider;

        public LogViewModelService(ILogger logger, IWorkContext workContext,
            ITranslationService translationService, IDateTimeService dateTimeService,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _workContext = workContext;
            _translationService = translationService;
            _dateTimeService = dateTimeService;
            _serviceProvider = serviceProvider;
        }

        public virtual LogListModel PrepareLogListModel()
        {
            var model = new LogListModel
            {
                AvailableLogLevels = LogLevel.Debug.ToSelectList(_translationService, _workContext, false).ToList()
            };
            model.AvailableLogLevels.Insert(0, new SelectListItem { Text = _translationService.GetResource("Admin.Common.All"), Value = " " });
            return model;
        }
        public virtual async Task<(IEnumerable<LogModel> logModels, int totalCount)> PrepareLogModel(LogListModel model, int pageIndex, int pageSize)
        {
            DateTime? createdOnFromValue = (model.CreatedOnFrom == null) ? null
                            : (DateTime?)_dateTimeService.ConvertToUtcTime(model.CreatedOnFrom.Value, _dateTimeService.CurrentTimeZone);

            DateTime? createdToFromValue = (model.CreatedOnTo == null) ? null
                            : (DateTime?)_dateTimeService.ConvertToUtcTime(model.CreatedOnTo.Value, _dateTimeService.CurrentTimeZone).AddDays(1);

            LogLevel? logLevel = model.LogLevelId > 0 ? (LogLevel?)(model.LogLevelId) : null;


            var logItems = await _logger.GetAllLogs(createdOnFromValue, createdToFromValue, model.Message,
                logLevel, pageIndex - 1, pageSize);
            return (logItems.Select(x => new LogModel
            {
                Id = x.Id,
                LogLevel = x.LogLevelId.GetTranslationEnum(_translationService, _workContext),
                ShortMessage = x.ShortMessage,
                FullMessage = "",
                IpAddress = x.IpAddress,
                CustomerId = x.CustomerId,
                PageUrl = x.PageUrl,
                ReferrerUrl = x.ReferrerUrl,
                CreatedOn = _dateTimeService.ConvertToUserTime(x.CreatedOnUtc, DateTimeKind.Utc)
            }), logItems.TotalCount);

        }
        public virtual async Task<LogModel> PrepareLogModel(Log log)
        {
            var model = new LogModel
            {
                Id = log.Id,
                LogLevel = log.LogLevelId.GetTranslationEnum(_translationService, _workContext),
                ShortMessage = log.ShortMessage,
                FullMessage = log.FullMessage,
                IpAddress = log.IpAddress,
                CustomerId = log.CustomerId,
                CustomerEmail = !String.IsNullOrEmpty(log.CustomerId) ? (await _serviceProvider.GetRequiredService<ICustomerService>().GetCustomerById(log.CustomerId))?.Email : "",
                PageUrl = log.PageUrl,
                ReferrerUrl = log.ReferrerUrl,
                CreatedOn = _dateTimeService.ConvertToUserTime(log.CreatedOnUtc, DateTimeKind.Utc)
            };
            return model;
        }
    }
}
