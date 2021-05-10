using Grand.Domain.Orders;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Grand.Business.Checkout.Interfaces.Orders
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
