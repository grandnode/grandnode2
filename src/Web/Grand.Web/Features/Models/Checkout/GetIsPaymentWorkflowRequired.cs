using Grand.Domain.Orders;
using MediatR;

namespace Grand.Web.Features.Models.Checkout
{
    public class GetIsPaymentWorkflowRequired : IRequest<bool>
    {
        public IList<ShoppingCartItem> Cart { get; set; }
        public bool? UseLoyaltyPoints { get; set; } = null;
    }
}
