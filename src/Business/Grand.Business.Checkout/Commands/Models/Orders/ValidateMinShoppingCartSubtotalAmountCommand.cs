using Grand.Domain.Customers;
using Grand.Domain.Orders;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Grand.Business.Checkout.Commands.Models.Orders
{
    public class ValidateMinShoppingCartSubtotalAmountCommand : IRequest<bool>
    {
        public Customer Customer { get; set; }
        public IList<ShoppingCartItem> Cart { get; set; }
    }
}
