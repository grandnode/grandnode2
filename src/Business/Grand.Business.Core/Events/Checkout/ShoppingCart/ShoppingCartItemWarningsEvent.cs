﻿using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using MediatR;

namespace Grand.Business.Core.Events.Checkout.ShoppingCart
{
    public class ShoppingCartItemWarningsEvent<C, S, P> : INotification where C : Customer where S : ShoppingCartItem where P : Product
    {
        private readonly IList<string> _warnings;
        private readonly C _customer;
        private readonly S _shoppingCartItem;
        private readonly P _product;

        public ShoppingCartItemWarningsEvent(IList<string> warnings, C customer, S shoppingCartItem, P product)
        {
            _warnings = warnings;
            _customer = customer;
            _shoppingCartItem = shoppingCartItem;
            _product = product;
        }
        public IList<string> Warnings => _warnings;
        public C Customer => _customer;
        public S ShoppingCartItem => _shoppingCartItem;
        public P Product => _product;
    }
}
