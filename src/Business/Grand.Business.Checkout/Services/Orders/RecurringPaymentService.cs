using Grand.Business.Core.Events.Checkout.Orders;
using Grand.Business.Core.Extensions;
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

    public virtual async Task<RecurringCycleClaim> ClaimCycle(string recurringPaymentId, string? providerReference,
        string cycleReference, double amount, DateTime paidOnUtc, DateTime? nextPaymentDateUtc)
    {
        ArgumentException.ThrowIfNullOrEmpty(cycleReference);

        RecurringPayment? recurringPayment;
        RecurringPaymentCycle cycle;
        var completed = false;

        //the reference is checked and the cycle recorded while no other delivery can do the same
        await using (await resourceLock.Acquire([LockKey(recurringPaymentId)]))
        {
            recurringPayment = await repository.GetByIdAsync(recurringPaymentId);
            if (recurringPayment == null)
                return new RecurringCycleClaim { Outcome = RecurringReportOutcome.NotFound };
            var refused = Refusal(recurringPayment, providerReference);
            if (refused == null && recurringPayment.Cycles.Any(x => x.ProviderReference == cycleReference))
                refused = RecurringReportOutcome.Duplicate;
            if (refused.HasValue)
                return new RecurringCycleClaim { Outcome = refused.Value, RecurringPayment = recurringPayment };

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

            KeepFirstReference(recurringPayment, providerReference);
            if (nextPaymentDateUtc.HasValue)
                recurringPayment.NextPaymentDateUtc = nextPaymentDateUtc;

            //LastError stays as history
            if (recurringPayment.Status is RecurringPaymentStatus.Pending or RecurringPaymentStatus.PastDue)
                recurringPayment.Status = RecurringPaymentStatus.Active;

            //the gateway took the money after a cancel, so the cycle is recorded and the status is left alone
            if (recurringPayment.TotalCycles > 0 && recurringPayment.CyclesPaid == recurringPayment.TotalCycles &&
                !recurringPayment.Status.HasEnded())
            {
                recurringPayment.Status = RecurringPaymentStatus.Completed;
                completed = true;
            }

            await repository.UpdateAsync(recurringPayment);
        }

        //event notification
        await mediator.EntityUpdated(recurringPayment);

        return new RecurringCycleClaim {
            Outcome = RecurringReportOutcome.Recorded, RecurringPayment = recurringPayment, Cycle = cycle,
            Completed = completed
        };
    }

    public virtual async Task<RecurringReportResult> RecordFailure(string recurringPaymentId,
        string? providerReference, string cycleReference, string? reason)
    {
        ArgumentException.ThrowIfNullOrEmpty(cycleReference);

        RecurringPayment? recurringPayment;
        bool enteredPastDue;

        await using (await resourceLock.Acquire([LockKey(recurringPaymentId)]))
        {
            recurringPayment = await repository.GetByIdAsync(recurringPaymentId);
            if (recurringPayment == null)
                return new RecurringReportResult { Outcome = RecurringReportOutcome.NotFound };
            var refused = Refusal(recurringPayment, providerReference);
            //a failure that arrives after the cycle was paid is a late, out of order event
            if (refused == null && recurringPayment.Cycles.Any(x => x.ProviderReference == cycleReference))
                refused = RecurringReportOutcome.Duplicate;
            if (refused == null && recurringPayment.Status.HasEnded())
            {
                //the gateway still tries to charge a payment the shop ended, typically one cancelled while pending:
                //its reference is kept so the gateway can be stopped
                if (KeepFirstReference(recurringPayment, providerReference))
                    await repository.UpdateAsync(recurringPayment);
                refused = RecurringReportOutcome.Ended;
            }

            if (refused.HasValue)
                return new RecurringReportResult { Outcome = refused.Value, RecurringPayment = recurringPayment };

            KeepFirstReference(recurringPayment, providerReference);
            recurringPayment.LastError = reason;
            recurringPayment.LastErrorDateUtc = DateTime.UtcNow;

            enteredPastDue = recurringPayment.Status != RecurringPaymentStatus.PastDue;
            recurringPayment.Status = RecurringPaymentStatus.PastDue;

            await repository.UpdateAsync(recurringPayment);
        }

        //event notification
        await mediator.EntityUpdated(recurringPayment);
        if (enteredPastDue)
            await mediator.Publish(new RecurringPaymentPastDueEvent(recurringPayment));

        return new RecurringReportResult { Outcome = RecurringReportOutcome.Recorded, RecurringPayment = recurringPayment };
    }

    public virtual async Task<RecurringReportResult> RecordEnded(string recurringPaymentId, string? providerReference)
    {
        RecurringPayment? recurringPayment;

        await using (await resourceLock.Acquire([LockKey(recurringPaymentId)]))
        {
            recurringPayment = await repository.GetByIdAsync(recurringPaymentId);
            //another subscription ending says nothing about the one this payment runs on
            if (recurringPayment == null)
                return new RecurringReportResult { Outcome = RecurringReportOutcome.NotFound };
            var refused = Refusal(recurringPayment, providerReference);
            if (refused == null && recurringPayment.Status.HasEnded())
                refused = RecurringReportOutcome.Ended;
            if (refused.HasValue)
                return new RecurringReportResult { Outcome = refused.Value, RecurringPayment = recurringPayment };

            KeepFirstReference(recurringPayment, providerReference);
            recurringPayment.Status = RecurringPaymentStatus.Cancelled;
            recurringPayment.CancelledOnUtc = DateTime.UtcNow;
            recurringPayment.CancelledBy = recurringPayment.PaymentMethodSystemName;

            await repository.UpdateAsync(recurringPayment);
        }

        //event notification
        await mediator.EntityUpdated(recurringPayment);
        await mediator.Publish(new RecurringPaymentCancelledEvent(recurringPayment));

        return new RecurringReportResult { Outcome = RecurringReportOutcome.Recorded, RecurringPayment = recurringPayment };
    }

    public virtual async Task<RecurringReportResult> RecordProviderReference(string recurringPaymentId,
        string? providerReference)
    {
        ArgumentException.ThrowIfNullOrEmpty(providerReference);

        RecurringPayment? recurringPayment;

        await using (await resourceLock.Acquire([LockKey(recurringPaymentId)]))
        {
            recurringPayment = await repository.GetByIdAsync(recurringPaymentId);
            if (recurringPayment == null)
                return new RecurringReportResult { Outcome = RecurringReportOutcome.NotFound };
            var refused = Refusal(recurringPayment, providerReference);
            if (refused == null && recurringPayment.ProviderReference == providerReference)
                refused = RecurringReportOutcome.Duplicate;
            if (refused.HasValue)
                return new RecurringReportResult { Outcome = refused.Value, RecurringPayment = recurringPayment };

            recurringPayment.ProviderReference = providerReference;
            await repository.UpdateAsync(recurringPayment);
        }

        //event notification
        await mediator.EntityUpdated(recurringPayment);

        return new RecurringReportResult { Outcome = RecurringReportOutcome.Recorded, RecurringPayment = recurringPayment };
    }

    //why a report of a payment found is refused whatever it reports, or null
    private static RecurringReportOutcome? Refusal(RecurringPayment recurringPayment, string? providerReference)
    {
        return recurringPayment.HasOtherProviderReference(providerReference)
            ? RecurringReportOutcome.OtherReference
            : null;
    }

    //the first reference reported is the one the payment runs on, whichever report brings it
    private static bool KeepFirstReference(RecurringPayment recurringPayment, string? providerReference)
    {
        if (!string.IsNullOrEmpty(recurringPayment.ProviderReference) || string.IsNullOrEmpty(providerReference))
            return false;

        recurringPayment.ProviderReference = providerReference;
        return true;
    }

    public virtual async Task<CancelRecurringPaymentResult> Cancel(string recurringPaymentId, string cancelledBy,
        Func<RecurringPayment, Task<CancelRecurringPaymentResult>> stopAtGateway)
    {
        ArgumentException.ThrowIfNullOrEmpty(cancelledBy);
        ArgumentNullException.ThrowIfNull(stopAtGateway);

        RecurringPayment? recurringPayment;
        var cancelled = false;

        await using (await resourceLock.Acquire([LockKey(recurringPaymentId)]))
        {
            recurringPayment = await repository.GetByIdAsync(recurringPaymentId);
            if (recurringPayment == null)
            {
                var notFound = new CancelRecurringPaymentResult();
                notFound.AddError("The recurring payment was not found");
                return notFound;
            }

            if (recurringPayment.Status.HasEnded())
                return new CancelRecurringPaymentResult();

            //a pending payment whose reference is not known yet has nothing to stop: the gateway is stopped when the
            //provider reports the reference (RecordRecurringPaymentReferenceCommand) or a first cycle on it
            if (recurringPayment.Status == RecurringPaymentStatus.Pending &&
                string.IsNullOrEmpty(recurringPayment.ProviderReference))
            {
                await MarkCancelled(recurringPayment, cancelledBy);
                cancelled = true;
            }
        }

        if (!cancelled)
        {
            //asked outside the lock: the call can be slow, and webhooks for this payment must not wait for it or
            //outlive its lease; every write below is made under the lock on a fresh read
            var result = await stopAtGateway(recurringPayment);

            await using (await resourceLock.Acquire([LockKey(recurringPaymentId)]))
            {
                recurringPayment = await repository.GetByIdAsync(recurringPaymentId);
                //a webhook or another cancel ended it meanwhile, which also explains a refusal from the gateway
                if (recurringPayment == null || recurringPayment.Status.HasEnded())
                    return new CancelRecurringPaymentResult();

                //never cancelled while the gateway may still charge
                if (!result.Success)
                    return result;

                await MarkCancelled(recurringPayment, cancelledBy);
            }
        }

        //event notification
        await mediator.EntityUpdated(recurringPayment);
        await mediator.Publish(new RecurringPaymentCancelledEvent(recurringPayment));

        return new CancelRecurringPaymentResult();
    }

    private Task MarkCancelled(RecurringPayment recurringPayment, string cancelledBy)
    {
        recurringPayment.Status = RecurringPaymentStatus.Cancelled;
        recurringPayment.CancelledOnUtc = DateTime.UtcNow;
        recurringPayment.CancelledBy = cancelledBy;

        return repository.UpdateAsync(recurringPayment);
    }

    private static string LockKey(string recurringPaymentId)
    {
        return $"{nameof(RecurringPayment)}:{recurringPaymentId}";
    }

    public virtual Task<RecurringPayment?> GetRecurringPaymentById(string id)
    {
        return repository.GetByIdAsync(id);
    }

    public virtual Task<RecurringPayment?> GetRecurringPaymentByInitialOrderId(string initialOrderId)
    {
        return repository.GetOneAsync(x => x.InitialOrderId == initialOrderId);
    }

    public virtual async Task<IPagedList<RecurringPayment>> GetCustomerRecurringPayments(string customerId,
        string storeId, int pageIndex = 0, int pageSize = int.MaxValue)
    {
        if (string.IsNullOrEmpty(customerId) || string.IsNullOrEmpty(storeId))
            return new PagedList<RecurringPayment>([], pageIndex, pageSize);

        var query = repository.Table
            .Where(x => x.CustomerId == customerId && x.StoreId == storeId)
            .OrderByDescending(x => x.CreatedOnUtc);

        return await repository.PagedAsync(query, pageIndex, pageSize);
    }

    public virtual async Task<bool> HasCustomerRecurringPayments(string customerId, string storeId)
    {
        if (string.IsNullOrEmpty(customerId) || string.IsNullOrEmpty(storeId))
            return false;

        return await repository.AnyAsync(repository.Table
            .Where(x => x.CustomerId == customerId && x.StoreId == storeId));
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
