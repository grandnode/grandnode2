using OrderEntity = Grand.Domain.Orders.Order;

namespace Order.ExternalOrderApi.Models;

public class OrderProcessResult
{
    public bool IsSuccess { get; set; }
    
    public OrderEntity? Order { get; set; }
    
    public IList<string> Errors { get; set; } = new List<string>();
}
