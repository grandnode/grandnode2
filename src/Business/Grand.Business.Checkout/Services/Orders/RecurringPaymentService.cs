using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Data;
using Grand.Domain;
using Grand.Domain.Orders;
using Grand.Infrastructure.Extensions;
using Grand.Mediator;

namespace Grand.Business.Checkout.Services.Orders;

public class RecurringPaymentService(
    IRepository<RecurringPayment> repository,
    IProductService productService,
    IMediator mediator,
    IResourceLock resourceLock)
    : IRecurringPaymentService
{
    public virtual async Task InsertRecurringPayment(RecurringPayment recurringPayment)
    {
        ArgumentNullException.ThrowIfNull(recurringPayment);

        await repository.InsertAsync(recurringPayment);

        //event notification
        await mediator.EntityInserted(recurringPayment);
    }

    public virtual async Task<RecurringCycleClaim> ClaimCycle(string recurringPaymentId, string providerReference,
        string cycleReference, double amount, DateTime paidOnUtc, DateTime? nextPaymentDateUtc)
    {
        ArgumentException.ThrowIfNullOrEmpty(cycleReference);

        RecurringPayment recurringPayment;
        RecurringPaymentCycle cycle;
        var completed = false;

        //the reference is checked and the cycle recorded while no other delivery can do the same
        await using (await resourceLock.Acquire([LockKey(recurringPaymentId)]))
        {
            recurringPayment = await repository.GetByIdAsync(recurringPaymentId);
            if (recurringPayment == null || recurringPayment.Cycles.Any(x => x.ProviderReference == cycleReference))
                return null;

            recurringPayment.CyclesPaid++;
            cycle = new RecurringPaymentCycle {
                CycleNumber = recurringPayment.CyclesPaid,
                //the first cycle is the order the customer placed
                OrderId = recurringPayment.CyclesPaid == 1 ? recurringPayment.InitialOrderId : UniqueIdentifier.New,
                ProviderReference = cycleReference,
                Amount = amount,
                PaidOnUtc = paidOnUtc
            };
            recurringPayment.Cycles.Add(cycle);

            if (string.IsNullOrEmpty(recurringPayment.ProviderReference))
                recurringPayment.ProviderReference = providerReference;
            if (nextPaymentDateUtc.HasValue)
                recurringPayment.NextPaymentDateUtc = nextPaymentDateUtc;

            //LastError stays as history
            if (recurringPayment.Status is RecurringPaymentStatus.Pending or RecurringPaymentStatus.PastDue)
                recurringPayment.Status = RecurringPaymentStatus.Active;

            //the gateway took the money after a cancel, so the cycle is recorded and the status is left alone
            if (recurringPayment.TotalCycles > 0 && recurringPayment.CyclesPaid == recurringPayment.TotalCycles &&
                recurringPayment.Status is not (RecurringPaymentStatus.Cancelled or RecurringPaymentStatus.Completed))
            {
                recurringPayment.Status = RecurringPaymentStatus.Completed;
                completed = true;
            }

            await repository.UpdateAsync(recurringPayment);
        }

        //event notification
        await mediator.EntityUpdated(recurringPayment);

        return new RecurringCycleClaim { RecurringPayment = recurringPayment, Cycle = cycle, Completed = completed };
    }

    public virtual async Task<bool> RecordFailure(string recurringPaymentId, string cycleReference, string reason)
    {
        ArgumentException.ThrowIfNullOrEmpty(cycleReference);

        RecurringPayment recurringPayment;
        bool enteredPastDue;

        await using (await resourceLock.Acquire([LockKey(recurringPaymentId)]))
        {
            recurringPayment = await repository.GetByIdAsync(recurringPaymentId);
            //a failure that arrives after the cycle was paid is a late, out of order event
            if (recurringPayment == null ||
                recurringPayment.Status is RecurringPaymentStatus.Cancelled or RecurringPaymentStatus.Completed ||
                recurringPayment.Cycles.Any(x => x.ProviderReference == cycleReference))
                return false;

            recurringPayment.LastError = reason;
            recurringPayment.LastErrorDateUtc = DateTime.UtcNow;

            enteredPastDue = recurringPayment.Status != RecurringPaymentStatus.PastDue;
            recurringPayment.Status = RecurringPaymentStatus.PastDue;

            await repository.UpdateAsync(recurringPayment);
        }

        //event notification
        await mediator.EntityUpdated(recurringPayment);

        return enteredPastDue;
    }

    public virtual async Task<bool> RecordEnded(string recurringPaymentId)
    {
        RecurringPayment recurringPayment;

        await using (await resourceLock.Acquire([LockKey(recurringPaymentId)]))
        {
            recurringPayment = await repository.GetByIdAsync(recurringPaymentId);
            if (recurringPayment == null ||
                recurringPayment.Status is RecurringPaymentStatus.Cancelled or RecurringPaymentStatus.Completed)
                return false;

            recurringPayment.Status = RecurringPaymentStatus.Cancelled;
            recurringPayment.CancelledOnUtc = DateTime.UtcNow;
            recurringPayment.CancelledBy = recurringPayment.PaymentMethodSystemName;

            await repository.UpdateAsync(recurringPayment);
        }

        //event notification
        await mediator.EntityUpdated(recurringPayment);

        return true;
    }

    public virtual async Task<CancelRecurringPaymentResult> Cancel(string recurringPaymentId, string cancelledBy,
        Func<RecurringPayment, Task<CancelRecurringPaymentResult>> stopAtGateway)
    {
        ArgumentException.ThrowIfNullOrEmpty(cancelledBy);
        ArgumentNullException.ThrowIfNull(stopAtGateway);

        RecurringPayment recurringPayment;

        //the gateway is asked under the lock, so a cycle reported meanwhile is recorded after the cancel and stopped again
        await using (await resourceLock.Acquire([LockKey(recurringPaymentId)]))
        {
            recurringPayment = await repository.GetByIdAsync(recurringPaymentId);
            if (recurringPayment == null)
            {
                var notFound = new CancelRecurringPaymentResult();
                notFound.AddError("The recurring payment was not found");
                return notFound;
            }

            if (recurringPayment.Status is RecurringPaymentStatus.Cancelled or RecurringPaymentStatus.Completed)
                return new CancelRecurringPaymentResult();

            //a pending payment has no reference to stop yet; the gateway may have started the subscription at checkout,
            //and the first cycle it reports, recorded on the cancelled payment, stops it
            if (recurringPayment.Status != RecurringPaymentStatus.Pending ||
                !string.IsNullOrEmpty(recurringPayment.ProviderReference))
            {
                var result = await stopAtGateway(recurringPayment);
                //never cancelled while the gateway may still charge
                if (!result.Success)
                    return result;
            }

            recurringPayment.Status = RecurringPaymentStatus.Cancelled;
            recurringPayment.CancelledOnUtc = DateTime.UtcNow;
            recurringPayment.CancelledBy = cancelledBy;

            await repository.UpdateAsync(recurringPayment);
        }

        //event notification
        await mediator.EntityUpdated(recurringPayment);

        return new CancelRecurringPaymentResult();
    }

    private static string LockKey(string recurringPaymentId)
    {
        return $"{nameof(RecurringPayment)}:{recurringPaymentId}";
    }

    public virtual Task<RecurringPayment> GetRecurringPaymentById(string id)
    {
        return repository.GetByIdAsync(id);
    }

    public virtual Task<RecurringPayment> GetRecurringPaymentByInitialOrderId(string initialOrderId)
    {
        return repository.GetOneAsync(x => x.InitialOrderId == initialOrderId);
    }

    public virtual async Task<bool> HasRecurringProducts(IEnumerable<ShoppingCartItem> cart)
    {
        ArgumentNullException.ThrowIfNull(cart);

        foreach (var sci in cart)
            if (await productService.GetProductById(sci.ProductId) is { IsRecurring: true })
                return true;

        return false;
    }
}
