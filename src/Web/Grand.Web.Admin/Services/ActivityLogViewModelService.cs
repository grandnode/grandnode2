using Grand.Business.Catalog.Interfaces.Categories;
using Grand.Business.Catalog.Interfaces.Collections;
using Grand.Business.Catalog.Interfaces.Products;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Logging;
using Grand.Business.Customers.Interfaces;
using Grand.Business.Marketing.Interfaces.Knowledgebase;
using Grand.Web.Admin.Extensions;
using Grand.Web.Admin.Interfaces;
using Grand.Web.Admin.Models.Logging;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Web.Admin.Services
{
    public partial class ActivityLogViewModelService : IActivityLogViewModelService
    {
        private readonly ICustomerActivityService _customerActivityService;
        private readonly IDateTimeService _dateTimeService;
        private readonly ICustomerService _customerService;
        private readonly ICategoryService _categoryService;
        private readonly ICollectionService _collectionService;
        private readonly IKnowledgebaseService _knowledgebaseService;
        private readonly IProductService _productService;

        public ActivityLogViewModelService(ICustomerActivityService customerActivityService,
            IDateTimeService dateTimeService, ICustomerService customerService,
            ICategoryService categoryService, ICollectionService collectionService,
            IProductService productService, IKnowledgebaseService knowledgebaseService)
        {
            _customerActivityService = customerActivityService;
            _dateTimeService = dateTimeService;
            _customerService = customerService;
            _categoryService = categoryService;
            _collectionService = collectionService;
            _productService = productService;
            _knowledgebaseService = knowledgebaseService;
        }
        public virtual async Task<IList<ActivityLogTypeModel>> PrepareActivityLogTypeModels()
        {
            var model = (await _customerActivityService
                .GetAllActivityTypes())
                .Select(x => x.ToModel())
                .ToList();
            return model;
        }
        public virtual async Task SaveTypes(List<string> types)
        {
            var activityTypes = await _customerActivityService.GetAllActivityTypes();
            foreach (var activityType in activityTypes)
            {
                activityType.Enabled = types.Contains(activityType.Id);
                await _customerActivityService.UpdateActivityType(activityType);
            }
        }
        public virtual async Task<ActivityLogSearchModel> PrepareActivityLogSearchModel()
        {
            var activityLogSearchModel = new ActivityLogSearchModel();
            activityLogSearchModel.ActivityLogType.Add(new SelectListItem
            {
                Value = "",
                Text = "All"
            });
            foreach (var at in await _customerActivityService.GetAllActivityTypes())
            {
                activityLogSearchModel.ActivityLogType.Add(new SelectListItem
                {
                    Value = at.Id.ToString(),
                    Text = at.Name
                });
            }
            return activityLogSearchModel;
        }

        public virtual async Task<(IEnumerable<ActivityLogModel> activityLogs, int totalCount)> PrepareActivityLogModel(ActivityLogSearchModel model, int pageIndex, int pageSize)
        {
            DateTime? startDateValue = (model.CreatedOnFrom == null) ? null
                : (DateTime?)_dateTimeService.ConvertToUtcTime(model.CreatedOnFrom.Value, _dateTimeService.CurrentTimeZone);

            DateTime? endDateValue = (model.CreatedOnTo == null) ? null
                            : (DateTime?)_dateTimeService.ConvertToUtcTime(model.CreatedOnTo.Value, _dateTimeService.CurrentTimeZone).AddDays(1);

            var activityLog = await _customerActivityService.GetAllActivities(model.Comment, startDateValue, endDateValue, null, model.ActivityLogTypeId, model.IpAddress, pageIndex - 1, pageSize);
            var activityLogModel = new List<ActivityLogModel>();
            foreach (var item in activityLog)
            {
                var customer = await _customerService.GetCustomerById(item.CustomerId);
                var cas = await _customerActivityService.GetActivityTypeById(item.ActivityLogTypeId);

                var m = item.ToModel();
                m.CreatedOn = _dateTimeService.ConvertToUserTime(item.CreatedOnUtc, DateTimeKind.Utc);
                m.ActivityLogTypeName = cas?.Name;
                m.CustomerEmail = customer != null ? customer.Email : "NULL";
                activityLogModel.Add(m);
            }
            return (activityLogModel, activityLog.TotalCount);
        }

        public virtual async Task<(IEnumerable<ActivityStatsModel> activityStats, int totalCount)> PrepareActivityStatModel(ActivityLogSearchModel model, int pageIndex, int pageSize)
        {
            DateTime? startDateValue = (model.CreatedOnFrom == null) ? null
                : (DateTime?)_dateTimeService.ConvertToUtcTime(model.CreatedOnFrom.Value, _dateTimeService.CurrentTimeZone);

            DateTime? endDateValue = (model.CreatedOnTo == null) ? null
                : (DateTime?)_dateTimeService.ConvertToUtcTime(model.CreatedOnTo.Value, _dateTimeService.CurrentTimeZone).AddDays(1);

            var activityStat = await _customerActivityService.GetStatsActivities(startDateValue, endDateValue, model.ActivityLogTypeId, pageIndex - 1, pageSize);
            var activityStatModel = new List<ActivityStatsModel>();
            foreach (var x in activityStat)
            {
                var activityLogType = await _customerActivityService.GetActivityTypeById(x.ActivityLogTypeId);
                string _name = "-empty-";
                if (activityLogType != null)
                {
                    IList<string> systemKeywordsCategory = new List<string>();
                    systemKeywordsCategory.Add("PublicStore.ViewCategory");
                    systemKeywordsCategory.Add("EditCategory");
                    systemKeywordsCategory.Add("AddNewCategory");

                    if (systemKeywordsCategory.Contains(activityLogType.SystemKeyword))
                    {
                        var category = await _categoryService.GetCategoryById(x.EntityKeyId);
                        if (category != null)
                            _name = category.Name;
                    }

                    IList<string> systemKeywordsCollection = new List<string>();
                    systemKeywordsCollection.Add("PublicStore.ViewCollection");
                    systemKeywordsCollection.Add("EditCollection");
                    systemKeywordsCollection.Add("AddNewCollection");

                    if (systemKeywordsCollection.Contains(activityLogType.SystemKeyword))
                    {
                        var collection = await _collectionService.GetCollectionById(x.EntityKeyId);
                        if (collection != null)
                            _name = collection.Name;
                    }

                    IList<string> systemKeywordsProduct = new List<string>();
                    systemKeywordsProduct.Add("PublicStore.ViewProduct");
                    systemKeywordsProduct.Add("EditProduct");
                    systemKeywordsProduct.Add("AddNewProduct");

                    if (systemKeywordsProduct.Contains(activityLogType.SystemKeyword))
                    {
                        var product = await _productService.GetProductById(x.EntityKeyId);
                        if (product != null)
                            _name = product.Name;
                    }
                    IList<string> systemKeywordsUrl = new List<string>();
                    systemKeywordsUrl.Add("PublicStore.Url");
                    if (systemKeywordsUrl.Contains(activityLogType.SystemKeyword))
                    {
                        _name = x.EntityKeyId;
                    }

                    IList<string> systemKeywordsKnowledgebaseCategory = new List<string>();
                    systemKeywordsKnowledgebaseCategory.Add("CreateKnowledgebaseCategory");
                    systemKeywordsKnowledgebaseCategory.Add("UpdateKnowledgebaseCategory");
                    systemKeywordsKnowledgebaseCategory.Add("DeleteKnowledgebaseCategory");

                    if (systemKeywordsKnowledgebaseCategory.Contains(activityLogType.SystemKeyword))
                    {
                        var category = await _knowledgebaseService.GetKnowledgebaseCategory(x.EntityKeyId);
                        if (category != null)
                            _name = category.Name;
                    }

                    IList<string> systemKeywordsKnowledgebaseArticle = new List<string>();
                    systemKeywordsKnowledgebaseArticle.Add("CreateKnowledgebaseArticle");
                    systemKeywordsKnowledgebaseArticle.Add("UpdateKnowledgebaseArticle");
                    systemKeywordsKnowledgebaseArticle.Add("DeleteKnowledgebaseArticle");

                    if (systemKeywordsKnowledgebaseArticle.Contains(activityLogType.SystemKeyword))
                    {
                        var article = await _knowledgebaseService.GetKnowledgebaseArticle(x.EntityKeyId);
                        if (article != null)
                            _name = article.Name;
                    }
                }

                var m = x.ToModel();
                m.ActivityLogTypeName = activityLogType != null ? activityLogType.Name : "-empty-";
                m.Name = _name;
                activityStatModel.Add(m);
            }
            return (activityStatModel, activityStat.TotalCount);
        }
    }
}
