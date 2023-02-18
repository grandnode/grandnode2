﻿using Grand.Domain.Catalog;
using MediatR;

namespace Grand.Business.Core.Events.Catalog
{
    public class UpdateProductOnCartEvent : INotification
    {
        private readonly Product _product;

        public UpdateProductOnCartEvent(Product product)
        {
            _product = product;
        }

        public Product Product => _product;
    }
}
