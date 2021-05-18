using Grand.Domain.Customers;
using Grand.Domain.Orders;
using MediatR;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace Grand.Business.Customers.Queries.Models
{
    public class GetCustomerQuery : IRequest<IQueryable<Customer>>
    {
        public DateTime? CreatedFromUtc { get; set; } = null;

        public DateTime? CreatedToUtc { get; set; } = null;
        public string AffiliateId { get; set; } = "";
        public string VendorId { get; set; } = "";
        public string StoreId { get; set; } = "";
        public string OwnerId { get; set; } = "";
        public string SalesEmployeeId { get; set; } = "";
        public string[] CustomerGroupIds { get; set; } = null;
        public string[] CustomerTagIds { get; set; } = null;
        public string Email { get; set; } = null;
        public string Username { get; set; } = null;
        public string FirstName { get; set; } = null;
        public string LastName { get; set; } = null;
        public string Company { get; set; } = null;
        public string Phone { get; set; } = null;
        public string ZipPostalCode { get; set; } = null;
        public bool LoadOnlyWithShoppingCart { get; set; } = false;
        public ShoppingCartType? Sct { get; set; } = null;
        public int PageIndex { get; set; } = 0;
        public int PageSize { get; set; } = int.MaxValue;
        public Expression<Func<Customer, object>> OrderBySelector { get; set; } = null;
    }
}
