﻿using Grand.Domain.Customers;
using Grand.Web.Admin.Models.Customers;

namespace Grand.Web.Admin.Interfaces
{
    public interface ICustomerAttributeViewModelService
    {
        Task<IEnumerable<CustomerAttributeModel>> PrepareCustomerAttributes();
        CustomerAttributeModel PrepareCustomerAttributeModel();
        CustomerAttributeModel PrepareCustomerAttributeModel(CustomerAttribute customerAttribute);
        Task<CustomerAttribute> InsertCustomerAttributeModel(CustomerAttributeModel model);
        Task<CustomerAttribute> UpdateCustomerAttributeModel(CustomerAttributeModel model, CustomerAttribute customerAttribute);
        Task DeleteCustomerAttribute(string id);
        Task DeleteCustomerAttributeValue(CustomerAttributeValueModel model);
        Task<IEnumerable<CustomerAttributeValueModel>> PrepareCustomerAttributeValues(string customerAttributeId);
        CustomerAttributeValueModel PrepareCustomerAttributeValueModel(string customerAttributeId);
        Task<CustomerAttributeValue> InsertCustomerAttributeValueModel(CustomerAttributeValueModel model);
        CustomerAttributeValueModel PrepareCustomerAttributeValueModel(CustomerAttributeValue customerAttributeValue);
        Task<CustomerAttributeValue> UpdateCustomerAttributeValueModel(CustomerAttributeValueModel model, CustomerAttributeValue customerAttributeValue);
    }
}
