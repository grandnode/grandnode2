using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Commands.Messages;
using Grand.Business.Core.Utilities.Messages.DotLiquidDrops;
using MediatR;

namespace Grand.Business.System.Commands.Handlers.Messages
{
    public class GetShipmentTokensCommandHandler : IRequestHandler<GetShipmentTokensCommand, LiquidShipment>
    {
        private readonly IProductService _productService;

        public GetShipmentTokensCommandHandler(
            IProductService productService)
        {
            _productService = productService;
        }

        public async Task<LiquidShipment> Handle(GetShipmentTokensCommand request, CancellationToken cancellationToken)
        {
            var liquidShipment = new LiquidShipment(request.Shipment, request.Order, request.Store, request.Host, request.Language);
            foreach (var shipmentItem in request.Shipment.ShipmentItems)
            {
                var orderitem = request.Order.OrderItems.FirstOrDefault(x => x.Id == shipmentItem.OrderItemId);
                var product = await _productService.GetProductById(shipmentItem.ProductId);
                var liquidshipmentItems = new LiquidShipmentItem(shipmentItem, request.Shipment, request.Order, orderitem, product, request.Language);
                liquidShipment.ShipmentItems.Add(liquidshipmentItems);
            }
            return liquidShipment;
        }
    }
}
