using Grand.Data;
using Grand.Domain.Customers;
using Grand.Infrastructure.Events;
using MediatR;

namespace Grand.Business.Authentication.Events;

public class CustomerDeletedEventHandler : INotificationHandler<EntityDeleted<Customer>>
{
    private readonly IRepository<ExternalAuthentication> _externalAuthenticationRepository;

    public CustomerDeletedEventHandler(IRepository<ExternalAuthentication> externalAuthenticationRepository)
    {
        _externalAuthenticationRepository = externalAuthenticationRepository;
    }

    public async Task Handle(EntityDeleted<Customer> notification, CancellationToken cancellationToken)
    {
        var externalRecords = _externalAuthenticationRepository.Table.Where(x => x.CustomerId == notification.Entity.Id)
            .ToList();
        foreach (var item in externalRecords) await _externalAuthenticationRepository.DeleteAsync(item);
    }
}