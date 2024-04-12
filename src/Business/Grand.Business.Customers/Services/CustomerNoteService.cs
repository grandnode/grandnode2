using Grand.Business.Core.Interfaces.Customers;
using Grand.Data;
using Grand.Domain.Customers;
using Grand.Infrastructure.Extensions;
using MediatR;

namespace Grand.Business.Customers.Services;

public class CustomerNoteService : ICustomerNoteService
{
    private readonly IRepository<CustomerNote> _customerNoteRepository;
    private readonly IMediator _mediator;

    public CustomerNoteService(IRepository<CustomerNote> customerNoteRepository,
        IMediator mediator)
    {
        _customerNoteRepository = customerNoteRepository;
        _mediator = mediator;
    }

    #region Customer note

    /// <summary>
    ///     Get note for customer
    /// </summary>
    /// <param name="id">Note identifier</param>
    /// <returns>CustomerNote</returns>
    public virtual Task<CustomerNote> GetCustomerNote(string id)
    {
        return string.IsNullOrWhiteSpace(id)
            ? Task.FromResult<CustomerNote>(null)
            : _customerNoteRepository.GetByIdAsync(id);
    }

    /// <summary>
    ///     Insert an customer note
    /// </summary>
    /// <param name="customerNote">The customer note</param>
    public virtual async Task InsertCustomerNote(CustomerNote customerNote)
    {
        ArgumentNullException.ThrowIfNull(customerNote);

        await _customerNoteRepository.InsertAsync(customerNote);

        //event notification
        await _mediator.EntityInserted(customerNote);
    }

    /// <summary>
    ///     Deletes an customer note
    /// </summary>
    /// <param name="customerNote">The customer note</param>
    public virtual async Task DeleteCustomerNote(CustomerNote customerNote)
    {
        ArgumentNullException.ThrowIfNull(customerNote);

        await _customerNoteRepository.DeleteAsync(customerNote);

        //event notification
        await _mediator.EntityDeleted(customerNote);
    }

    /// <summary>
    ///     Get notes for customer
    /// </summary>
    /// <param name="customerId">Customer identifier</param>
    /// <param name="displayToCustomer">Display to customer</param>
    /// <returns>OrderNote</returns>
    public virtual async Task<IList<CustomerNote>> GetCustomerNotes(string customerId, bool? displayToCustomer = null)
    {
        var query = from customerNote in _customerNoteRepository.Table
            where customerNote.CustomerId == customerId
            select customerNote;

        if (displayToCustomer.HasValue)
            query = query.Where(x => x.DisplayToCustomer == displayToCustomer.Value);

        query = query.OrderByDescending(x => x.CreatedOnUtc);

        return await Task.FromResult(query.ToList());
    }

    #endregion
}