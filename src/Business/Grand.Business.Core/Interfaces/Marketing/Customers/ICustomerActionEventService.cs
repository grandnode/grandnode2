﻿using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Orders;

namespace Grand.Business.Core.Interfaces.Marketing.Customers
{
    public partial interface ICustomerActionEventService
    {
        /// <summary>
        /// Run action add to cart 
        /// </summary>
        Task AddToCart(ShoppingCartItem cart, Product product, Customer customer);

        /// <summary>
        /// Run action add new order / paid order
        /// </summary>
        Task AddOrder(Order order, CustomerActionTypeEnum customerActionType);

        /// <summary>
        /// Viewed
        /// </summary>
        Task Viewed(Customer customer, string currentUrl, string previousUrl);

        /// <summary>
        /// Run action url
        /// </summary>
        Task Url(Customer customer, string currentUrl, string previousUrl);


        /// <summary>
        /// Run action url
        /// </summary>
        Task Registration(Customer customer);
    }
}
