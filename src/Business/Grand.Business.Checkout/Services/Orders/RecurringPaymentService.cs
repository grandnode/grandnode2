using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Data;
using Grand.Domain.Orders;
using Grand.Infrastructure.Extensions;
using Grand.Mediator;

namespace Grand.Business.Checkout.Services.Orders;

public class RecurringPaymentService(IRepository<RecurringPayment> repository, IMediator mediator)
    : IRecurringPaymentService
{
    public virtual async Task InsertRecurringPayment(RecurringPayment recurringPayment)
    {
        ArgumentNullException.ThrowIfNull(recurringPayment);

        await repository.InsertAsync(recurringPayment);

        //event notification
        await mediator.EntityInserted(recurringPayment);
    }

    public virtual async Task UpdateRecurringPayment(RecurringPayment recurringPayment)
    {
        ArgumentNullException.ThrowIfNull(recurringPayment);

        await repository.UpdateAsync(recurringPayment);

        //event notification
        await mediator.EntityUpdated(recurringPayment);
    }

    public virtual Task<RecurringPayment> GetRecurringPaymentById(string id)
    {
        return repository.GetByIdAsync(id);
    }

    public virtual Task<RecurringPayment> GetRecurringPaymentByInitialOrderId(string initialOrderId)
    {
        return repository.GetOneAsync(x => x.InitialOrderId == initialOrderId);
    }
}
