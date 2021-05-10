using Grand.Business.Catalog.Interfaces.Products;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Interfaces.Logging;
using Grand.Business.Common.Interfaces.Stores;
using Grand.Business.Customers.Interfaces;
using Grand.Business.Marketing.Interfaces.Banners;
using Grand.Business.Marketing.Interfaces.Contacts;
using Grand.Business.Marketing.Interfaces.Customers;
using Grand.Business.Messages.Interfaces;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Web.Admin.Extensions;
using Grand.Web.Admin.Interfaces;
using Grand.Web.Admin.Models.Catalog;
using Grand.Web.Admin.Models.Customers;
using Grand.Web.Common.Extensions;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Grand.Domain.Customers.CustomerAction;

namespace Grand.Web.Admin.Services
{
    public partial class CustomerActionViewModelService : ICustomerActionViewModelService
    {
        private readonly ICustomerService _customerService;
        private readonly IGroupService _groupService;
        private readonly ICustomerTagService _customerTagService;
        private readonly ITranslationService _translationService;
        private readonly ICustomerActivityService _customerActivityService;
        private readonly IStoreService _storeService;
        private readonly IVendorService _vendorService;
        private readonly ICustomerActionService _customerActionService;
        private readonly IBannerService _bannerService;
        private readonly IInteractiveFormService _interactiveFormService;
        private readonly IMessageTemplateService _messageTemplateService;
        private readonly IDateTimeService _dateTimeService;
        private readonly IProductService _productService;

        public CustomerActionViewModelService(ICustomerService customerService,
            IGroupService groupService,
            ICustomerTagService customerTagService,
            ITranslationService translationService,
            ICustomerActivityService customerActivityService,
            IStoreService storeService,
            IVendorService vendorService,
            ICustomerActionService customerActionService,
            IBannerService bannerService,
            IInteractiveFormService interactiveFormService,
            IMessageTemplateService messageTemplateService,
            IDateTimeService dateTimeService,
            IProductService productService)
        {
            _customerService = customerService;
            _groupService = groupService;
            _customerTagService = customerTagService;
            _translationService = translationService;
            _customerActivityService = customerActivityService;
            _storeService = storeService;
            _vendorService = vendorService;
            _customerActionService = customerActionService;
            _bannerService = bannerService;
            _interactiveFormService = interactiveFormService;
            _messageTemplateService = messageTemplateService;
            _dateTimeService = dateTimeService;
            _productService = productService;
        }

        public virtual async Task PrepareReactObjectModel(CustomerActionModel model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            var banners = await _bannerService.GetAllBanners();
            foreach (var item in banners)
            {
                model.Banners.Add(new SelectListItem
                {
                    Text = item.Name,
                    Value = item.Id.ToString()
                });

            }
            var message = await _messageTemplateService.GetAllMessageTemplates("");
            foreach (var item in message)
            {
                model.MessageTemplates.Add(new SelectListItem
                {
                    Text = item.Name,
                    Value = item.Id.ToString()
                });
            }
            var customerGroup = await _groupService.GetAllCustomerGroups();
            foreach (var item in customerGroup)
            {
                model.CustomerGroups.Add(new SelectListItem
                {
                    Text = item.Name,
                    Value = item.Id.ToString()
                });
            }

            var customerTag = await _customerTagService.GetAllCustomerTags();
            foreach (var item in customerTag)
            {
                model.CustomerTags.Add(new SelectListItem
                {
                    Text = item.Name,
                    Value = item.Id.ToString()
                });
            }

            foreach (var item in await _customerActionService.GetCustomerActionType())
            {
                model.ActionType.Add(new SelectListItem()
                {
                    Text = item.Name,
                    Value = item.Id.ToString()
                });
            }

            foreach (var item in await _interactiveFormService.GetAllForms())
            {
                model.InteractiveForms.Add(new SelectListItem()
                {
                    Text = item.Name,
                    Value = item.Id.ToString()
                });

            }


        }

        public virtual async Task<SerializeCustomerActionHistory> PrepareHistoryModelForList(CustomerActionHistory history)
        {
            var customer = await _customerService.GetCustomerById(history.CustomerId);
            return new SerializeCustomerActionHistory
            {
                Email = customer != null ? String.IsNullOrEmpty(customer.Email) ? "(unknown)" : customer.Email : "(unknown)",
                CreateDate = _dateTimeService.ConvertToUserTime(history.CreateDateUtc, DateTimeKind.Utc),
            };
        }

        public virtual async Task<CustomerActionModel> PrepareCustomerActionModel()
        {
            var model = new CustomerActionModel();
            model.Active = true;
            model.StartDateTime = DateTime.Now;
            model.EndDateTime = DateTime.Now.AddMonths(1);
            model.ReactionTypeId = (int)CustomerReactionTypeEnum.Banner;
            await PrepareReactObjectModel(model);
            return model;
        }
        public virtual async Task<CustomerAction> InsertCustomerActionModel(CustomerActionModel model)
        {
            var customeraction = model.ToEntity(_dateTimeService);
            await _customerActionService.InsertCustomerAction(customeraction);
            await _customerActivityService.InsertActivity("AddNewCustomerAction", customeraction.Id, _translationService.GetResource("ActivityLog.AddNewCustomerAction"), customeraction.Name);
            return customeraction;
        }
        public virtual async Task<CustomerAction> UpdateCustomerActionModel(CustomerAction customeraction, CustomerActionModel model)
        {
            if (customeraction.Conditions.Count() > 0)
                model.ActionTypeId = customeraction.ActionTypeId;
            if (String.IsNullOrEmpty(model.ActionTypeId))
                model.ActionTypeId = customeraction.ActionTypeId;

            customeraction = model.ToEntity(customeraction, _dateTimeService);
            await _customerActionService.UpdateCustomerAction(customeraction);
            await _customerActivityService.InsertActivity("EditCustomerAction", customeraction.Id, _translationService.GetResource("ActivityLog.EditCustomerAction"), customeraction.Name);
            return customeraction;
        }

        public virtual async Task<CustomerActionConditionModel> PrepareCustomerActionConditionModel(string customerActionId)
        {
            var customerAction = await _customerActionService.GetCustomerActionById(customerActionId);
            var actionType = await _customerActionService.GetCustomerActionTypeById(customerAction.ActionTypeId);

            var model = new CustomerActionConditionModel();
            model.CustomerActionId = customerActionId;

            foreach (var item in actionType.ConditionType)
            {
                model.CustomerActionConditionType.Add(new SelectListItem()
                {
                    Value = item.ToString(),
                    Text = ((CustomerActionConditionTypeEnum)item).ToString()
                });
            }
            return model;
        }

        public virtual async Task<(string customerActionId, string conditionId)> InsertCustomerActionConditionModel(CustomerActionConditionModel model)
        {
            var customerAction = await _customerActionService.GetCustomerActionById(model.CustomerActionId);
            if (customerAction == null)
            {
                return (null, null);
            }
            var condition = new CustomerAction.ActionCondition()
            {
                Name = model.Name,
                CustomerActionConditionTypeId = model.CustomerActionConditionTypeId,
                ConditionId = model.ConditionId,
            };
            customerAction.Conditions.Add(condition);
            await _customerActionService.UpdateCustomerAction(customerAction);

            await _customerActivityService.InsertActivity("AddNewCustomerActionCondition", customerAction.Id, _translationService.GetResource("ActivityLog.AddNewCustomerAction"), customerAction.Name);
            return (customerAction.Id, condition.Id);
        }

        public virtual async Task<CustomerAction> UpdateCustomerActionConditionModel(CustomerAction customeraction, ActionCondition condition, CustomerActionConditionModel model)
        {
            condition = model.ToEntity(condition);
            await _customerActionService.UpdateCustomerAction(customeraction);
            //activity log
            await _customerActivityService.InsertActivity("EditCustomerActionCondition", customeraction.Id, _translationService.GetResource("ActivityLog.EditCustomerActionCondition"), customeraction.Name);
            return customeraction;
        }

        public virtual async Task ConditionDelete(string Id, string customerActionId)
        {
            var customerAction = await _customerActionService.GetCustomerActionById(customerActionId);
            var condition = customerAction.Conditions.FirstOrDefault(x => x.Id == Id);
            customerAction.Conditions.Remove(condition);
            await _customerActionService.UpdateCustomerAction(customerAction);
        }

        public virtual async Task ConditionDeletePosition(string id, string customerActionId, string conditionId)
        {
            var customerActions = await _customerActionService.GetCustomerActionById(customerActionId);
            var condition = customerActions.Conditions.FirstOrDefault(x => x.Id == conditionId);

            if (condition.CustomerActionConditionTypeId == CustomerActionConditionTypeEnum.Product)
            {
                condition.Products.Remove(id);
                await _customerActionService.UpdateCustomerAction(customerActions);
            }
            if (condition.CustomerActionConditionTypeId == CustomerActionConditionTypeEnum.Category)
            {
                condition.Categories.Remove(id);
                await _customerActionService.UpdateCustomerAction(customerActions);
            }
            if (condition.CustomerActionConditionTypeId == CustomerActionConditionTypeEnum.Collection)
            {
                condition.Collections.Remove(id);
                await _customerActionService.UpdateCustomerAction(customerActions);
            }
            if (condition.CustomerActionConditionTypeId == CustomerActionConditionTypeEnum.Vendor)
            {
                condition.Vendors.Remove(id);
                await _customerActionService.UpdateCustomerAction(customerActions);
            }
            if (condition.CustomerActionConditionTypeId == CustomerActionConditionTypeEnum.ProductAttribute)
            {
                condition.ProductAttribute.Remove(condition.ProductAttribute.FirstOrDefault(x => x.Id == id));
                await _customerActionService.UpdateCustomerAction(customerActions);
            }
            if (condition.CustomerActionConditionTypeId == CustomerActionConditionTypeEnum.ProductSpecification)
            {
                condition.ProductSpecifications.Remove(condition.ProductSpecifications.FirstOrDefault(x => x.Id == id));
                await _customerActionService.UpdateCustomerAction(customerActions);
            }
            if (condition.CustomerActionConditionTypeId == CustomerActionConditionTypeEnum.CustomerGroup)
            {
                condition.CustomerGroups.Remove(id);
                await _customerActionService.UpdateCustomerAction(customerActions);
            }
            if (condition.CustomerActionConditionTypeId == CustomerActionConditionTypeEnum.CustomerTag)
            {
                condition.CustomerTags.Remove(id);
                await _customerActionService.UpdateCustomerAction(customerActions);
            }

            if (condition.CustomerActionConditionTypeId == CustomerActionConditionTypeEnum.CustomCustomerAttribute)
            {
                condition.CustomCustomerAttributes.Remove(condition.CustomCustomerAttributes.FirstOrDefault(x => x.Id == id));
                await _customerActionService.UpdateCustomerAction(customerActions);
            }
            if (condition.CustomerActionConditionTypeId == CustomerActionConditionTypeEnum.CustomerRegisterField)
            {
                condition.CustomerRegistration.Remove(condition.CustomerRegistration.FirstOrDefault(x => x.Id == id));
                await _customerActionService.UpdateCustomerAction(customerActions);
            }
            if (condition.CustomerActionConditionTypeId == CustomerActionConditionTypeEnum.UrlCurrent)
            {
                condition.UrlCurrent.Remove(condition.UrlCurrent.FirstOrDefault(x => x.Id == id));
                await _customerActionService.UpdateCustomerAction(customerActions);
            }
            if (condition.CustomerActionConditionTypeId == CustomerActionConditionTypeEnum.UrlReferrer)
            {
                condition.UrlReferrer.Remove(condition.UrlReferrer.FirstOrDefault(x => x.Id == id));
                await _customerActionService.UpdateCustomerAction(customerActions);
            }
            if (condition.CustomerActionConditionTypeId == CustomerActionConditionTypeEnum.Store)
            {
                condition.Stores.Remove(id);
                await _customerActionService.UpdateCustomerAction(customerActions);
            }

        }

        public virtual async Task<CustomerActionConditionModel.AddProductToConditionModel> PrepareAddProductToConditionModel(string customerActionId, string conditionId)
        {
            var model = new CustomerActionConditionModel.AddProductToConditionModel();
            model.CustomerActionConditionId = conditionId;
            model.CustomerActionId = customerActionId;

            //stores
            model.AvailableStores.Add(new SelectListItem { Text = _translationService.GetResource("Admin.Common.All"), Value = " " });
            foreach (var s in await _storeService.GetAllStores())
                model.AvailableStores.Add(new SelectListItem { Text = s.Shortcut, Value = s.Id.ToString() });

            //vendors
            model.AvailableVendors.Add(new SelectListItem { Text = _translationService.GetResource("Admin.Common.All"), Value = " " });
            foreach (var v in await _vendorService.GetAllVendors(showHidden: true))
                model.AvailableVendors.Add(new SelectListItem { Text = v.Name, Value = v.Id.ToString() });

            //product types
            model.AvailableProductTypes = ProductType.SimpleProduct.ToSelectList().ToList();
            model.AvailableProductTypes.Insert(0, new SelectListItem { Text = _translationService.GetResource("Admin.Common.All"), Value = " " });

            return model;
        }
        public virtual async Task InsertProductToConditionModel(CustomerActionConditionModel.AddProductToConditionModel model)
        {
            foreach (string id in model.SelectedProductIds)
            {
                var customerAction = await _customerActionService.GetCustomerActionById(model.CustomerActionId);
                if (customerAction != null)
                {
                    var condition = customerAction.Conditions.FirstOrDefault(x => x.Id == model.CustomerActionConditionId);
                    if (condition != null)
                    {
                        if (condition.Products.Where(x => x == id).Count() == 0)
                        {
                            condition.Products.Add(id);
                            await _customerActionService.UpdateCustomerAction(customerAction);
                        }
                    }
                }
            }
        }
        public virtual async Task InsertCategoryConditionModel(CustomerActionConditionModel.AddCategoryConditionModel model)
        {
            foreach (string id in model.SelectedCategoryIds)
            {
                var customerAction = await _customerActionService.GetCustomerActionById(model.CustomerActionId);
                if (customerAction != null)
                {
                    var condition = customerAction.Conditions.FirstOrDefault(x => x.Id == model.CustomerActionConditionId);
                    if (condition != null)
                    {
                        if (condition.Categories.Where(x => x == id).Count() == 0)
                        {
                            condition.Categories.Add(id);
                            await _customerActionService.UpdateCustomerAction(customerAction);
                        }
                    }
                }
            }
        }
        public virtual async Task InsertCollectionConditionModel(CustomerActionConditionModel.AddCollectionConditionModel model)
        {
            foreach (string id in model.SelectedCollectionIds)
            {
                var customerAction = await _customerActionService.GetCustomerActionById(model.CustomerActionId);
                if (customerAction != null)
                {
                    var condition = customerAction.Conditions.FirstOrDefault(x => x.Id == model.CustomerActionConditionId);
                    if (condition != null)
                    {
                        if (condition.Collections.Where(x => x == id).Count() == 0)
                        {
                            condition.Collections.Add(id);
                            await _customerActionService.UpdateCustomerAction(customerAction);
                        }
                    }
                }
            }
        }
        public virtual async Task InsertCustomerGroupConditionModel(CustomerActionConditionModel.AddCustomerGroupConditionModel model)
        {
            var customerAction = await _customerActionService.GetCustomerActionById(model.CustomerActionId);
            if (customerAction != null)
            {
                var condition = customerAction.Conditions.FirstOrDefault(x => x.Id == model.ConditionId);
                if (condition != null)
                {
                    if (condition.CustomerGroups.Where(x => x == model.CustomerGroupId).Count() == 0)
                    {
                        condition.CustomerGroups.Add(model.CustomerGroupId);
                        await _customerActionService.UpdateCustomerAction(customerAction);
                    }
                }
            }
        }

        public virtual async Task InsertStoreConditionModel(CustomerActionConditionModel.AddStoreConditionModel model)
        {
            var customerAction = await _customerActionService.GetCustomerActionById(model.CustomerActionId);
            if (customerAction != null)
            {
                var condition = customerAction.Conditions.FirstOrDefault(x => x.Id == model.ConditionId);
                if (condition != null)
                {
                    if (condition.Stores.Where(x => x == model.StoreId).Count() == 0)
                    {
                        condition.Stores.Add(model.StoreId);
                        await _customerActionService.UpdateCustomerAction(customerAction);
                    }
                }
            }
        }
        public virtual async Task InsertVendorConditionModel(CustomerActionConditionModel.AddVendorConditionModel model)
        {
            var customerAction = await _customerActionService.GetCustomerActionById(model.CustomerActionId);
            if (customerAction != null)
            {
                var condition = customerAction.Conditions.FirstOrDefault(x => x.Id == model.ConditionId);
                if (condition != null)
                {
                    if (condition.Vendors.Where(x => x == model.VendorId).Count() == 0)
                    {
                        condition.Vendors.Add(model.VendorId);
                        await _customerActionService.UpdateCustomerAction(customerAction);
                    }
                }
            }
        }

        public virtual async Task InsertCustomerTagConditionModel(CustomerActionConditionModel.AddCustomerTagConditionModel model)
        {
            var customerAction = await _customerActionService.GetCustomerActionById(model.CustomerActionId);
            if (customerAction != null)
            {
                var condition = customerAction.Conditions.FirstOrDefault(x => x.Id == model.ConditionId);
                if (condition != null)
                {
                    if (condition.CustomerTags.Where(x => x == model.CustomerTagId).Count() == 0)
                    {
                        condition.CustomerTags.Add(model.CustomerTagId);
                        await _customerActionService.UpdateCustomerAction(customerAction);
                    }
                }
            }
        }
        public virtual async Task InsertProductAttributeConditionModel(CustomerActionConditionModel.AddProductAttributeConditionModel model)
        {
            var customerAction = await _customerActionService.GetCustomerActionById(model.CustomerActionId);
            if (customerAction != null)
            {
                var condition = customerAction.Conditions.FirstOrDefault(x => x.Id == model.ConditionId);
                if (condition != null)
                {
                    var _pv = new CustomerAction.ActionCondition.ProductAttributeValue()
                    {
                        ProductAttributeId = model.ProductAttributeId,
                        Name = model.Name
                    };
                    condition.ProductAttribute.Add(_pv);
                    await _customerActionService.UpdateCustomerAction(customerAction);
                }
            }
        }
        public virtual async Task UpdateProductAttributeConditionModel(CustomerActionConditionModel.AddProductAttributeConditionModel model)
        {
            var customerAction = await _customerActionService.GetCustomerActionById(model.CustomerActionId);
            if (customerAction != null)
            {
                var condition = customerAction.Conditions.FirstOrDefault(x => x.Id == model.ConditionId);
                if (condition != null)
                {
                    var pva = condition.ProductAttribute.FirstOrDefault(x => x.Id == model.Id);
                    pva.ProductAttributeId = model.ProductAttributeId;
                    pva.Name = model.Name;
                    await _customerActionService.UpdateCustomerAction(customerAction);
                }
            }
        }
        public virtual async Task InsertProductSpecificationConditionModel(CustomerActionConditionModel.AddProductSpecificationConditionModel model)
        {
            var customerAction = await _customerActionService.GetCustomerActionById(model.CustomerActionId);
            if (customerAction != null)
            {
                var condition = customerAction.Conditions.FirstOrDefault(x => x.Id == model.ConditionId);
                if (condition != null)
                {
                    if (condition.ProductSpecifications.Where(x => x.ProductSpecyficationId == model.SpecificationId && x.ProductSpecyficationValueId == model.SpecificationValueId).Count() == 0)
                    {
                        var _ps = new CustomerAction.ActionCondition.ProductSpecification()
                        {
                            ProductSpecyficationId = model.SpecificationId,
                            ProductSpecyficationValueId = model.SpecificationValueId
                        };
                        condition.ProductSpecifications.Add(_ps);
                        await _customerActionService.UpdateCustomerAction(customerAction);
                    }
                }
            }
        }
        public virtual async Task InsertCustomerRegisterConditionModel(CustomerActionConditionModel.AddCustomerRegisterConditionModel model)
        {
            var customerAction = await _customerActionService.GetCustomerActionById(model.CustomerActionId);
            if (customerAction != null)
            {
                var condition = customerAction.Conditions.FirstOrDefault(x => x.Id == model.ConditionId);
                if (condition != null)
                {
                    var _cr = new CustomerAction.ActionCondition.CustomerRegister()
                    {
                        RegisterField = model.CustomerRegisterName,
                        RegisterValue = model.CustomerRegisterValue,
                    };
                    condition.CustomerRegistration.Add(_cr);
                    await _customerActionService.UpdateCustomerAction(customerAction);
                }
            }
        }
        public virtual async Task UpdateCustomerRegisterConditionModel(CustomerActionConditionModel.AddCustomerRegisterConditionModel model)
        {
            var customerAction = await _customerActionService.GetCustomerActionById(model.CustomerActionId);
            if (customerAction != null)
            {
                var condition = customerAction.Conditions.FirstOrDefault(x => x.Id == model.ConditionId);
                if (condition != null)
                {
                    var cr = condition.CustomerRegistration.FirstOrDefault(x => x.Id == model.Id);
                    cr.RegisterField = model.CustomerRegisterName;
                    cr.RegisterValue = model.CustomerRegisterValue;
                    await _customerActionService.UpdateCustomerAction(customerAction);
                }
            }
        }
        public virtual async Task InsertCustomCustomerAttributeConditionModel(CustomerActionConditionModel.AddCustomCustomerAttributeConditionModel model)
        {
            var customerAction = await _customerActionService.GetCustomerActionById(model.CustomerActionId);
            if (customerAction != null)
            {
                var condition = customerAction.Conditions.FirstOrDefault(x => x.Id == model.ConditionId);
                if (condition != null)
                {
                    var _cr = new CustomerAction.ActionCondition.CustomerRegister()
                    {
                        RegisterField = model.CustomerAttributeName,
                        RegisterValue = model.CustomerAttributeValue,
                    };
                    condition.CustomCustomerAttributes.Add(_cr);
                    await _customerActionService.UpdateCustomerAction(customerAction);
                }
            }
        }
        public virtual async Task UpdateCustomCustomerAttributeConditionModel(CustomerActionConditionModel.AddCustomCustomerAttributeConditionModel model)
        {
            var customerAction = await _customerActionService.GetCustomerActionById(model.CustomerActionId);
            if (customerAction != null)
            {
                var condition = customerAction.Conditions.FirstOrDefault(x => x.Id == model.ConditionId);
                if (condition != null)
                {
                    var cr = condition.CustomCustomerAttributes.FirstOrDefault(x => x.Id == model.Id);
                    cr.RegisterField = model.CustomerAttributeName;
                    cr.RegisterValue = model.CustomerAttributeValue;
                    await _customerActionService.UpdateCustomerAction(customerAction);
                }
            }
        }

        public virtual async Task InsertUrlConditionModel(CustomerActionConditionModel.AddUrlConditionModel model)
        {
            var customerAction = await _customerActionService.GetCustomerActionById(model.CustomerActionId);
            if (customerAction != null)
            {
                var condition = customerAction.Conditions.FirstOrDefault(x => x.Id == model.ConditionId);
                if (condition != null)
                {
                    var _url = new CustomerAction.ActionCondition.Url()
                    {
                        Name = model.Name
                    };
                    condition.UrlReferrer.Add(_url);
                    await _customerActionService.UpdateCustomerAction(customerAction);
                }
            }
        }

        public virtual async Task UpdateUrlConditionModel(CustomerActionConditionModel.AddUrlConditionModel model)
        {
            var customerAction = await _customerActionService.GetCustomerActionById(model.CustomerActionId);
            if (customerAction != null)
            {
                var condition = customerAction.Conditions.FirstOrDefault(x => x.Id == model.ConditionId);
                if (condition != null)
                {
                    var _url = condition.UrlReferrer.FirstOrDefault(x => x.Id == model.Id);
                    _url.Name = model.Name;
                    await _customerActionService.UpdateCustomerAction(customerAction);
                }
            }
        }
        public virtual async Task InsertUrlCurrentConditionModel(CustomerActionConditionModel.AddUrlConditionModel model)
        {
            var customerAction = await _customerActionService.GetCustomerActionById(model.CustomerActionId);
            if (customerAction != null)
            {
                var condition = customerAction.Conditions.FirstOrDefault(x => x.Id == model.ConditionId);
                if (condition != null)
                {
                    var _url = new CustomerAction.ActionCondition.Url()
                    {
                        Name = model.Name
                    };
                    condition.UrlCurrent.Add(_url);
                    await _customerActionService.UpdateCustomerAction(customerAction);
                }
            }
        }
        public virtual async Task UpdateUrlCurrentConditionModel(CustomerActionConditionModel.AddUrlConditionModel model)
        {
            var customerAction = await _customerActionService.GetCustomerActionById(model.CustomerActionId);
            if (customerAction != null)
            {
                var condition = customerAction.Conditions.FirstOrDefault(x => x.Id == model.ConditionId);
                if (condition != null)
                {
                    var _url = condition.UrlCurrent.FirstOrDefault(x => x.Id == model.Id);
                    _url.Name = model.Name;
                    await _customerActionService.UpdateCustomerAction(customerAction);
                }
            }
        }
        public virtual async Task<(IList<ProductModel> products, int totalCount)> PrepareProductModel(CustomerActionConditionModel.AddProductToConditionModel model, int pageIndex, int pageSize)
        {
            var products = await _productService.PrepareProductList(model.SearchCategoryId, model.SearchBrandId, model.SearchCollectionId, model.SearchStoreId, model.SearchVendorId, model.SearchProductTypeId, model.SearchProductName, pageIndex, pageSize);
            return (products.Select(x => x.ToModel(_dateTimeService)).ToList(), products.TotalCount);
        }
    }
}
