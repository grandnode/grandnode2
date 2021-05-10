using Grand.Business.Catalog.Interfaces.Products;
using Grand.Business.Checkout.Interfaces.Orders;
using Grand.Business.Common.Extensions;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Messages.Interfaces;
using Grand.Infrastructure;
using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Grand.Web.Commands.Models.Orders;
using Grand.Web.Models.Orders;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Grand.Business.Common.Interfaces.Directory;

namespace Grand.Web.Commands.Handler.Orders
{
    public class MerchandiseReturnSubmitCommandHandler : IRequestHandler<MerchandiseReturnSubmitCommand, (MerchandiseReturnModel model, MerchandiseReturn rr)>
    {
        private readonly IWorkContext _workContext;
        private readonly IProductService _productService;
        private readonly IMerchandiseReturnService _merchandiseReturnService;
        private readonly IMessageProviderService _messageProviderService;
        private readonly ITranslationService _translationService;
        private readonly IGroupService _groupService;
        private readonly LanguageSettings _languageSettings;


        public MerchandiseReturnSubmitCommandHandler(IWorkContext workContext,
            IProductService productService,
            IMerchandiseReturnService merchandiseReturnService,
            IMessageProviderService messageProviderService,
            ITranslationService translationService,
            IGroupService groupService,
            LanguageSettings languageSettings)
        {
            _workContext = workContext;
            _productService = productService;
            _merchandiseReturnService = merchandiseReturnService;
            _messageProviderService = messageProviderService;
            _translationService = translationService;
            _groupService = groupService;
            _languageSettings = languageSettings;
        }

        public async Task<(MerchandiseReturnModel model, MerchandiseReturn rr)> Handle(MerchandiseReturnSubmitCommand request, CancellationToken cancellationToken)
        {
            var rr = new MerchandiseReturn
            {
                StoreId = _workContext.CurrentStore.Id,
                OrderId = request.Order.Id,
                CustomerId = _workContext.CurrentCustomer.Id,
                OwnerId = await _groupService.IsOwner(_workContext.CurrentCustomer) ? _workContext.CurrentCustomer.Id : _workContext.CurrentCustomer.OwnerId,
                SeId = request.Order.SeId,
                CustomerComments = request.Model.Comments,
                StaffNotes = string.Empty,
                MerchandiseReturnStatus = MerchandiseReturnStatus.Pending,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow,
                PickupAddress = request.Address,
            };

            if (request.Model.PickupDate.HasValue)
                rr.PickupDate = request.Model.PickupDate.Value;
            var vendors = new List<string>();
            foreach (var orderItem in request.Order.OrderItems)
            {
                var product = await _productService.GetProductById(orderItem.ProductId);
                if (!product.NotReturnable)
                {
                    int quantity = 0; //parse quantity
                    string rrrId = "";
                    string rraId = "";

                    foreach (string formKey in request.Form.Keys)
                    {
                        if (formKey.Equals(string.Format("quantity{0}", orderItem.Id), StringComparison.OrdinalIgnoreCase))
                        {
                            int.TryParse(request.Form[formKey], out quantity);
                        }

                        if (formKey.Equals(string.Format("reason{0}", orderItem.Id), StringComparison.OrdinalIgnoreCase))
                        {
                            rrrId = request.Form[formKey];
                        }

                        if (formKey.Equals(string.Format("action{0}", orderItem.Id), StringComparison.OrdinalIgnoreCase))
                        {
                            rraId = request.Form[formKey];
                        }
                    }

                    if (quantity > 0)
                    {
                        var rrr = await _merchandiseReturnService.GetMerchandiseReturnReasonById(rrrId);
                        var rra = await _merchandiseReturnService.GetMerchandiseReturnActionById(rraId);
                        rr.MerchandiseReturnItems.Add(new MerchandiseReturnItem
                        {
                            RequestedAction = rra != null ? rra.GetTranslation(x => x.Name, _workContext.WorkingLanguage.Id) : "not available",
                            ReasonForReturn = rrr != null ? rrr.GetTranslation(x => x.Name, _workContext.WorkingLanguage.Id) : "not available",
                            Quantity = quantity,
                            OrderItemId = orderItem.Id
                        });
                        rr.VendorId = orderItem.VendorId;
                        vendors.Add(orderItem.VendorId);
                    }
                }
            }
            if (vendors.Distinct().Count() > 1)
            {
                request.Model.Error = _translationService.GetResource("MerchandiseReturns.MultiVendorsItems");
                return (request.Model, rr);
            }
            if (rr.MerchandiseReturnItems.Any())
            {
                await _merchandiseReturnService.InsertMerchandiseReturn(rr);

                //notify store owner here (email)
                await _messageProviderService.SendNewMerchandiseReturnStoreOwnerMessage(rr, request.Order, _languageSettings.DefaultAdminLanguageId);
                //notify customer
                await _messageProviderService.SendNewMerchandiseReturnCustomerMessage(rr, request.Order, request.Order.CustomerLanguageId);
            }
            else
            {
                request.Model.Error = _translationService.GetResource("MerchandiseReturns.NoItemsSubmitted");
            }

            return (request.Model, rr);
        }
    }
}
