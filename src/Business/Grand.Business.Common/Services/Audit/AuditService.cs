using Grand.Business.Core.Interfaces.Common.Audit;
using Grand.Data;
using Grand.Data.Audit;
using Grand.Domain;
using Grand.Domain.Audit;
using Grand.Infrastructure.Audit;
using Grand.Mediator;
using System.Runtime.CompilerServices;

namespace Grand.Business.Common.Services.Audit;

public class AuditService(
    IRepository<AuditEntry> repository,
    IAuditEntryWriter writer,
    IMediator mediator) : IAuditService
{
    private const int StreamPageSize = 1000;

    public Task<IPagedList<AuditEntry>> GetEntries(AuditEntrySearch search, int pageIndex = 0,
        int pageSize = int.MaxValue)
    {
        return repository.PagedAsync(Query(search), pageIndex, pageSize);
    }

    public Task<AuditEntry> GetById(string id)
    {
        return repository.GetByIdAsync(id);
    }

    public async IAsyncEnumerable<AuditEntry> StreamEntries(AuditEntrySearch search,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        //entries written while the export runs would shift the pages, so the export stops at its start time
        var now = DateTime.UtcNow;
        var bounded = new AuditEntrySearch {
            FromUtc = search.FromUtc,
            ToUtc = search.ToUtc == null || search.ToUtc > now ? now : search.ToUtc,
            ActorEmail = search.ActorEmail,
            Source = search.Source,
            Kind = search.Kind,
            EntityType = search.EntityType,
            EntityId = search.EntityId,
            EntityNames = search.EntityNames,
            StoreId = search.StoreId,
            OperationId = search.OperationId
        };

        for (var pageIndex = 0;; pageIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var page = await repository.PagedAsync(Query(bounded), pageIndex, StreamPageSize, cancellationToken);
            foreach (var entry in page)
                yield return entry;

            if (page.Count < StreamPageSize)
                yield break;
        }
    }

    public async Task<IList<string>> GetEntityTypes()
    {
        var types = await repository.ToListAsync(repository.Table.Select(x => x.EntityType).Distinct());
        return types.Where(t => !string.IsNullOrEmpty(t)).OrderBy(t => t, StringComparer.Ordinal).ToList();
    }

    public async Task InsertEvent(AuditEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        if (entry.CreatedOnUtc == default)
            entry.CreatedOnUtc = DateTime.UtcNow;

        await writer.InsertManyAsync([entry]);
        await mediator.Publish(new AuditEntryCreated(entry));
    }

    public async Task<int> DeleteOlderThan(DateTime createdBeforeUtc)
    {
        var count = await repository.CountAsync(repository.Table.Where(x => x.CreatedOnUtc < createdBeforeUtc));
        if (count > 0)
            await repository.DeleteManyAsync(x => x.CreatedOnUtc < createdBeforeUtc);
        return count;
    }

    private IQueryable<AuditEntry> Query(AuditEntrySearch search)
    {
        var query = repository.Table;
        if (search.FromUtc.HasValue)
            query = query.Where(x => x.CreatedOnUtc >= search.FromUtc.Value);
        if (search.ToUtc.HasValue)
            query = query.Where(x => x.CreatedOnUtc <= search.ToUtc.Value);
        if (!string.IsNullOrEmpty(search.ActorEmail))
        {
            var email = search.ActorEmail.ToLowerInvariant();
            query = query.Where(x => x.ActorEmail != null && x.ActorEmail.ToLower().Contains(email));
        }

        if (search.Source.HasValue)
            query = query.Where(x => x.Source == search.Source.Value);
        if (search.Kind.HasValue)
            query = query.Where(x => x.Kind == search.Kind.Value);
        if (!string.IsNullOrEmpty(search.EntityType))
            query = query.Where(x => x.EntityType == search.EntityType);
        if (!string.IsNullOrEmpty(search.EntityId))
            query = query.Where(x => x.EntityId == search.EntityId);
        if (search.EntityNames is { Count: > 0 })
        {
            var names = search.EntityNames.ToList();
            query = query.Where(x => names.Contains(x.EntityName));
        }

        if (!string.IsNullOrEmpty(search.StoreId))
            query = query.Where(x => x.StoreId == search.StoreId);
        if (!string.IsNullOrEmpty(search.OperationId))
            query = query.Where(x => x.OperationId == search.OperationId);

        return query.OrderByDescending(x => x.CreatedOnUtc).ThenByDescending(x => x.Id);
    }
}
