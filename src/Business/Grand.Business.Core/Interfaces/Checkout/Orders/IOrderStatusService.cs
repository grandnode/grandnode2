using Grand.Domain.Orders;

namespace Grand.Business.Core.Interfaces.Checkout.Orders
{
    public interface IOrderStatusService
    {
        Task<IList<OrderStatus>> GetAll();
        Task<OrderStatus> GetById(string id);
        Task<OrderStatus> GetByStatusId(int statusId);
        Task Insert(OrderStatus orderStatus);
        Task Update(OrderStatus orderStatus);
        Task Delete(OrderStatus orderStatus);
    }
}
