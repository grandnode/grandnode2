using Grand.Domain.Orders;
using Grand.Web.AdminShared.Models.Orders;

namespace Grand.Web.AdminShared.Interfaces;

public interface IOrderNoteViewModelService
{
    Task<IList<OrderModel.OrderNote>> PrepareOrderNotes(Order order);
    Task InsertOrderNote(Order order, string downloadId, bool displayToCustomer, string message);
    Task DeleteOrderNote(Order order, string id);
}
