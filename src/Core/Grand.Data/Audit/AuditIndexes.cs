using Grand.Domain.Audit;

namespace Grand.Data.Audit;

/// <summary>
///     The indexes of the audit log, shared by the installer and the 2.5 migration. Each ends with the
///     "newest first, then id" order every query uses, so the server never sorts in memory.
/// </summary>
public static class AuditIndexes
{
    public static async Task Create(IDatabaseContext databaseContext, IRepository<AuditEntry> repository)
    {
        await databaseContext.CreateIndex(repository,
            OrderBuilder<AuditEntry>.Create().Descending(x => x.CreatedOnUtc).Descending(x => x.Id),
            "CreatedOnUtc_Id");
        await databaseContext.CreateIndex(repository,
            OrderBuilder<AuditEntry>.Create().Ascending(x => x.EntityType).Ascending(x => x.EntityId)
                .Descending(x => x.CreatedOnUtc).Descending(x => x.Id), "EntityType_EntityId_CreatedOnUtc_Id");
        await databaseContext.CreateIndex(repository,
            OrderBuilder<AuditEntry>.Create().Ascending(x => x.OwnerType).Ascending(x => x.OwnerId)
                .Descending(x => x.CreatedOnUtc).Descending(x => x.Id), "OwnerType_OwnerId_CreatedOnUtc_Id");
        await databaseContext.CreateIndex(repository,
            OrderBuilder<AuditEntry>.Create().Ascending(x => x.ActorCustomerId).Descending(x => x.CreatedOnUtc)
                .Descending(x => x.Id), "ActorCustomerId_CreatedOnUtc_Id");
        await databaseContext.CreateIndex(repository,
            OrderBuilder<AuditEntry>.Create().Ascending(x => x.OperationId), "OperationId");
    }
}
