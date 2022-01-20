﻿using Grand.Business.Catalog.Interfaces.Products;
using Grand.Business.Customers.Interfaces;
using Grand.Business.Marketing.Commands.Models;
using Grand.Infrastructure;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Data;
using Grand.Domain.Logging;
using Grand.SharedKernel.Extensions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Grand.Business.Marketing.Commands.Handlers
{
    public class CustomerActionEventConditionCommandHandler : IRequestHandler<CustomerActionEventConditionCommand, bool>
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IProductService _productService;
        private readonly ICustomerService _customerService;
        private readonly IRepository<ActivityLog> _activityLogRepository;
        private readonly IRepository<ActivityLogType> _activityLogTypeRepository;

        public CustomerActionEventConditionCommandHandler(
            IServiceProvider serviceProvider,
            IProductService productService,
            ICustomerService customerService,
            IRepository<ActivityLog> activityLogRepository,
            IRepository<ActivityLogType> activityLogTypeRepository)
        {
            _serviceProvider = serviceProvider;
            _productService = productService;
            _customerService = customerService;
            _activityLogRepository = activityLogRepository;
            _activityLogTypeRepository = activityLogTypeRepository;
        }

        public async Task<bool> Handle(CustomerActionEventConditionCommand request, CancellationToken cancellationToken)
        {
            return await Condition(
                request.CustomerActionTypes,
                request.Action,
                request.ProductId,
                request.Attributes,
                request.CustomerId,
                request.CurrentUrl,
                request.PreviousUrl);
        }

        protected async Task<bool> Condition(IList<CustomerActionType> customerActionTypes, CustomerAction action, string productId, IList<CustomAttribute> customAttributes, string customerId, string currentUrl, string previousUrl)
        {
            if (!action.Conditions.Any())
                return true;

            bool cond = false;
            foreach (var item in action.Conditions)
            {
                #region product
                if (!string.IsNullOrEmpty(productId))
                {
                    var product = await _productService.GetProductById(productId);

                    if (item.CustomerActionConditionTypeId == CustomerActionConditionTypeEnum.Category)
                    {
                        cond = ConditionCategory(item, product.ProductCategories);
                    }

                    if (item.CustomerActionConditionTypeId == CustomerActionConditionTypeEnum.Collection)
                    {
                        cond = ConditionCollection(item, product.ProductCollections);
                    }

                    if (item.CustomerActionConditionTypeId == CustomerActionConditionTypeEnum.Product)
                    {
                        cond = ConditionProducts(item, product.Id);
                    }

                    if (item.CustomerActionConditionTypeId == CustomerActionConditionTypeEnum.ProductAttribute)
                    {
                        if (customAttributes != null && customAttributes.Any())
                        {
                            cond = ConditionProductAttribute(item, product, customAttributes);
                        }
                    }

                    if (item.CustomerActionConditionTypeId == CustomerActionConditionTypeEnum.ProductSpecification)
                    {
                        cond = ConditionSpecificationAttribute(item, product.ProductSpecificationAttributes);
                    }

                    if (item.CustomerActionConditionTypeId == CustomerActionConditionTypeEnum.Vendor)
                    {
                        cond = ConditionVendors(item, product.VendorId);
                    }

                }
                #endregion

                #region Action type viewed
                if (action.ActionTypeId == customerActionTypes.FirstOrDefault(x => x.SystemKeyword == "Viewed").Id)
                {
                    cond = false;
                    if (item.CustomerActionConditionTypeId == CustomerActionConditionTypeEnum.Category)
                    {
                        var _actLogType = (from a in _activityLogTypeRepository.Table
                                           where a.SystemKeyword == "PublicStore.ViewCategory"
                                           select a).FirstOrDefault();
                        if (_actLogType != null)
                        {
                            if (_actLogType.Enabled)
                            {
                                var productCategory = (from p in _activityLogRepository.Table
                                                       where p.CustomerId == customerId && p.ActivityLogTypeId == _actLogType.Id
                                                       select p.EntityKeyId).Distinct().ToList();
                                cond = ConditionCategory(item, productCategory);
                            }
                        }
                    }

                    if (item.CustomerActionConditionTypeId == CustomerActionConditionTypeEnum.Collection)
                    {
                        cond = false;
                        var _actLogType = (from a in _activityLogTypeRepository.Table
                                           where a.SystemKeyword == "PublicStore.ViewCollection"
                                           select a).FirstOrDefault();
                        if (_actLogType != null)
                        {
                            if (_actLogType.Enabled)
                            {
                                var productCollection = (from p in _activityLogRepository.Table
                                                           where p.CustomerId == customerId && p.ActivityLogTypeId == _actLogType.Id
                                                           select p.EntityKeyId).Distinct().ToList();
                                cond = ConditionCollection(item, productCollection);
                            }
                        }
                    }

                    if (item.CustomerActionConditionTypeId == CustomerActionConditionTypeEnum.Product)
                    {
                        cond = false;
                        var _actLogType = (from a in _activityLogTypeRepository.Table
                                           where a.SystemKeyword == "PublicStore.ViewProduct"
                                           select a).FirstOrDefault();
                        if (_actLogType != null)
                        {
                            if (_actLogType.Enabled)
                            {
                                var products = (from p in _activityLogRepository.Table
                                                where p.CustomerId == customerId && p.ActivityLogTypeId == _actLogType.Id
                                                select p.EntityKeyId).Distinct().ToList();
                                cond = ConditionProducts(item, products);
                            }
                        }
                    }
                }
                #endregion

                var customer = await _customerService.GetCustomerById(customerId);

                if (item.CustomerActionConditionTypeId == CustomerActionConditionTypeEnum.CustomerGroup)
                {
                    cond = ConditionCustomerGroup(item, customer);
                }

                if (item.CustomerActionConditionTypeId == CustomerActionConditionTypeEnum.CustomerTag)
                {
                    cond = ConditionCustomerTag(item, customer);
                }

                if (item.CustomerActionConditionTypeId == CustomerActionConditionTypeEnum.CustomerRegisterField)
                {
                    cond = await ConditionCustomerRegister(item, customer);
                }

                if (item.CustomerActionConditionTypeId == CustomerActionConditionTypeEnum.CustomCustomerAttribute)
                {
                    cond = await ConditionCustomerAttribute(item, customer);
                }

                if (item.CustomerActionConditionTypeId == CustomerActionConditionTypeEnum.UrlCurrent)
                {
                    cond = item.UrlCurrent.Select(x => x.Name).Contains(currentUrl);
                }

                if (item.CustomerActionConditionTypeId == CustomerActionConditionTypeEnum.UrlReferrer)
                {
                    cond = item.UrlReferrer.Select(x => x.Name).Contains(previousUrl);
                }

                if (item.CustomerActionConditionTypeId == CustomerActionConditionTypeEnum.Store)
                {
                    var workContext = _serviceProvider.GetRequiredService<IWorkContext>();
                    cond = ConditionStores(item, workContext.CurrentStore.Id);
                }

                if (action.ConditionId == CustomerActionConditionEnum.OneOfThem && cond)
                    return true;
                if (action.ConditionId == CustomerActionConditionEnum.AllOfThem && !cond)
                    return false;
            }

            return cond;
        }
        protected bool ConditionCategory(CustomerAction.ActionCondition condition, ICollection<ProductCategory> categorties)
        {
            bool cond = true;
            if (condition.ConditionId == CustomerActionConditionEnum.AllOfThem)
            {
                cond = categorties.Select(x => x.CategoryId).ContainsAll(condition.Categories);
            }
            if (condition.ConditionId == CustomerActionConditionEnum.OneOfThem)
            {
                cond = categorties.Select(x => x.CategoryId).ContainsAny(condition.Categories);
            }

            return cond;
        }
        protected bool ConditionCategory(CustomerAction.ActionCondition condition, ICollection<string> categorties)
        {
            bool cond = true;
            if (condition.ConditionId == CustomerActionConditionEnum.AllOfThem)
            {
                cond = categorties.ContainsAll(condition.Categories);
            }
            if (condition.ConditionId == CustomerActionConditionEnum.OneOfThem)
            {
                cond = categorties.ContainsAny(condition.Categories);
            }

            return cond;
        }
        protected bool ConditionCollection(CustomerAction.ActionCondition condition, ICollection<ProductCollection> collections)
        {
            bool cond = true;

            if (condition.ConditionId == CustomerActionConditionEnum.AllOfThem)
            {
                cond = collections.Select(x => x.CollectionId).ContainsAll(condition.Collections);
            }
            if (condition.ConditionId == CustomerActionConditionEnum.OneOfThem)
            {
                cond = collections.Select(x => x.CollectionId).ContainsAny(condition.Collections);
            }

            return cond;
        }
        protected bool ConditionCollection(CustomerAction.ActionCondition condition, ICollection<string> collections)
        {
            bool cond = true;

            if (condition.ConditionId == CustomerActionConditionEnum.AllOfThem)
            {
                cond = collections.ContainsAll(condition.Collections);
            }
            if (condition.ConditionId == CustomerActionConditionEnum.OneOfThem)
            {
                cond = collections.ContainsAny(condition.Collections);
            }

            return cond;
        }
        protected bool ConditionProducts(CustomerAction.ActionCondition condition, string productId)
        {
            return condition.Products.Contains(productId);
        }
        protected bool ConditionStores(CustomerAction.ActionCondition condition, string storeId)
        {
            return condition.Stores.Contains(storeId);
        }
        protected bool ConditionProducts(CustomerAction.ActionCondition condition, ICollection<string> products)
        {
            bool cond = true;
            if (condition.ConditionId == CustomerActionConditionEnum.AllOfThem)
            {
                cond = products.ContainsAll(condition.Products);
            }
            if (condition.ConditionId == CustomerActionConditionEnum.OneOfThem)
            {
                cond = products.ContainsAny(condition.Products);
            }

            return cond;
        }
        protected bool ConditionProductAttribute(CustomerAction.ActionCondition condition, Product product, IList<CustomAttribute> customAttributes)
        {
            bool cond = false;
            var productAttributeParser = _serviceProvider.GetRequiredService<IProductAttributeParser>();
            if (condition.ConditionId == CustomerActionConditionEnum.OneOfThem)
            {
                var attributes = productAttributeParser.ParseProductAttributeMappings(product, customAttributes);
                foreach (var attr in attributes)
                {
                    var attributeValuesStr = productAttributeParser.ParseValues(customAttributes, attr.Id);
                    foreach (var attrV in attributeValuesStr)
                    {
                        var attrsv = attr.ProductAttributeValues.Where(x => x.Id == attrV).FirstOrDefault();
                        if (attrsv != null)
                            if (condition.ProductAttribute.Where(x => x.ProductAttributeId == attr.ProductAttributeId && x.Name == attrsv.Name).Count() > 0)
                            {
                                cond = true;
                            }
                    }
                }
            }
            if (condition.ConditionId == CustomerActionConditionEnum.AllOfThem)
            {
                cond = true;
                foreach (var itemPA in condition.ProductAttribute)
                {
                    var attributes = productAttributeParser.ParseProductAttributeMappings(product, customAttributes);
                    if (attributes.Where(x => x.ProductAttributeId == itemPA.ProductAttributeId).Count() > 0)
                    {
                        cond = false;
                        foreach (var attr in attributes.Where(x => x.ProductAttributeId == itemPA.ProductAttributeId))
                        {
                            var attributeValuesStr = productAttributeParser.ParseValues(customAttributes, attr.Id);
                            foreach (var attrV in attributeValuesStr)
                            {
                                var attrsv = attr.ProductAttributeValues.Where(x => x.Id == attrV).FirstOrDefault();
                                if (attrsv != null)
                                {
                                    if (attrsv.Name == itemPA.Name)
                                    {
                                        cond = true;
                                    }
                                }
                                else
                                {
                                    return false;
                                }
                            }
                        }
                        if (!cond)
                            return false;
                    }
                    else
                    {
                        return false;
                    }
                }
            }

            return cond;
        }
        protected bool ConditionSpecificationAttribute(CustomerAction.ActionCondition condition, ICollection<ProductSpecificationAttribute> productspecificationattribute)
        {
            bool cond = false;

            if (condition.ConditionId == CustomerActionConditionEnum.AllOfThem)
            {
                cond = true;
                foreach (var spec in condition.ProductSpecifications)
                {
                    if (productspecificationattribute.Where(x => x.SpecificationAttributeId == spec.ProductSpecyficationId && x.SpecificationAttributeOptionId == spec.ProductSpecyficationValueId).Count() == 0)
                        cond = false;
                }
            }
            if (condition.ConditionId == CustomerActionConditionEnum.OneOfThem)
            {
                foreach (var spec in productspecificationattribute)
                {
                    if (condition.ProductSpecifications.Where(x => x.ProductSpecyficationId == spec.SpecificationAttributeId && x.ProductSpecyficationValueId == spec.SpecificationAttributeOptionId).Count() > 0)
                        cond = true;
                }
            }

            return cond;
        }
        protected bool ConditionVendors(CustomerAction.ActionCondition condition, string vendorId)
        {
            return condition.Vendors.Contains(vendorId);
        }
        protected bool ConditionCustomerGroup(CustomerAction.ActionCondition condition, Customer customer)
        {
            bool cond = false;
            if (customer != null)
            {
                var customerGroups = customer.Groups;
                if (condition.ConditionId == CustomerActionConditionEnum.AllOfThem)
                {
                    cond = customerGroups.Select(x => x).ContainsAll(condition.CustomerGroups);
                }
                if (condition.ConditionId == CustomerActionConditionEnum.OneOfThem)
                {
                    cond = customerGroups.Select(x => x).ContainsAny(condition.CustomerGroups);
                }
            }
            return cond;
        }
        protected bool ConditionCustomerTag(CustomerAction.ActionCondition condition, Customer customer)
        {
            bool cond = false;
            if (customer != null)
            {
                var customerTags = customer.CustomerTags;
                if (condition.ConditionId == CustomerActionConditionEnum.AllOfThem)
                {
                    cond = customerTags.Select(x => x).ContainsAll(condition.CustomerTags);
                }
                if (condition.ConditionId == CustomerActionConditionEnum.OneOfThem)
                {
                    cond = customerTags.Select(x => x).ContainsAny(condition.CustomerTags);
                }
            }
            return cond;
        }
        protected async Task<bool> ConditionCustomerRegister(CustomerAction.ActionCondition condition, Customer customer)
        {
            bool cond = false;
            if (customer != null)
            {
                var _userFields = customer.UserFields;
                if (condition.ConditionId == CustomerActionConditionEnum.AllOfThem)
                {
                    cond = true;
                    foreach (var item in condition.CustomerRegistration)
                    {
                        if (_userFields.Where(x => x.Key == item.RegisterField && x.Value.ToLower() == item.RegisterValue.ToLower()).Count() == 0)
                            cond = false;
                    }
                }
                if (condition.ConditionId == CustomerActionConditionEnum.OneOfThem)
                {
                    foreach (var item in condition.CustomerRegistration)
                    {
                        if (_userFields.Where(x => x.Key == item.RegisterField && x.Value.ToLower() == item.RegisterValue.ToLower()).Count() > 0)
                            cond = true;
                    }
                }
            }
            return await Task.FromResult(cond);
        }
        protected async Task<bool> ConditionCustomerAttribute(CustomerAction.ActionCondition condition, Customer customer)
        {
            bool cond = false;
            if (customer != null)
            {
                var customerAttributeParser = _serviceProvider.GetRequiredService<ICustomerAttributeParser>();

                if (condition.ConditionId == CustomerActionConditionEnum.AllOfThem)
                {
                    if (customer.Attributes.Any())
                    {
                        var selectedValues = await customerAttributeParser.ParseCustomerAttributeValues(customer.Attributes);
                        cond = true;
                        foreach (var item in condition.CustomCustomerAttributes)
                        {
                            var _fields = item.RegisterField.Split(':');
                            if (_fields.Count() > 1)
                            {
                                if (selectedValues.Where(x => x.CustomerAttributeId == _fields.FirstOrDefault() && x.Id == _fields.LastOrDefault()).Count() == 0)
                                    cond = false;
                            }
                            else
                                cond = false;
                        }
                    }
                }
                if (condition.ConditionId == CustomerActionConditionEnum.OneOfThem)
                {
                    if (customer.Attributes.Any())
                    {
                        var selectedValues = await customerAttributeParser.ParseCustomerAttributeValues(customer.Attributes);
                        foreach (var item in condition.CustomCustomerAttributes)
                        {
                            var _fields = item.RegisterField.Split(':');
                            if (_fields.Count() > 1)
                            {
                                if (selectedValues.Where(x => x.CustomerAttributeId == _fields.FirstOrDefault() && x.Id == _fields.LastOrDefault()).Count() > 0)
                                    cond = true;
                            }
                        }
                    }
                }
            }
            return cond;
        }

    }
}
