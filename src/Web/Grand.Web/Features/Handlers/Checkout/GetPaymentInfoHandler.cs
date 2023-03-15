﻿using Grand.Web.Features.Models.Checkout;
using Grand.Web.Models.Checkout;
using MediatR;
using Microsoft.AspNetCore.Routing;

namespace Grand.Web.Features.Handlers.Checkout
{
    public class GetPaymentInfoHandler : IRequestHandler<GetPaymentInfo, CheckoutPaymentInfoModel>
    {
        private readonly LinkGenerator _linkGenerator;

        public GetPaymentInfoHandler(LinkGenerator linkGenerator)
        {
            _linkGenerator = linkGenerator;
        }

        public async Task<CheckoutPaymentInfoModel> Handle(GetPaymentInfo request, CancellationToken cancellationToken)
        {
            var url = _linkGenerator.GetPathByRouteValues(routeName: await request.PaymentMethod.GetControllerRouteName(), values: null);
            var model = new CheckoutPaymentInfoModel {
                PaymentUrl = url
            };
            return await Task.FromResult(model);
        }
    }
}
