using Grand.Data;
using Grand.Domain.Customers;
using Grand.Infrastructure.Events;
using Grand.Mediator;

namespace Grand.Business.Authentication.Events;

/// <summary>
///     A customer deleted in the admin panel leaves SCIM: its record and its group memberships go, so the identity provider can
///     provision the user again as a new account
/// </summary>
public class ScimCustomerDeletedEventHandler(IRepository<ScimUser> users, IRepository<ScimGroup> groups)
    : INotificationHandler<EntityDeleted<Customer>>
{
    public async Task Handle(EntityDeleted<Customer> notification, CancellationToken cancellationToken)
    {
        var customerId = notification.Entity.Id;
        var record = await users.GetByIdAsync(customerId);
        if (record != null)
            await users.DeleteAsync(record);

        foreach (var group in await groups.ToListAsync(groups.Table.Where(x => x.Members.Contains(customerId)),
                     cancellationToken))
            await groups.RemoveCollectionFieldItem(group.Id, x => x.Members, m => m == customerId);
    }
}
