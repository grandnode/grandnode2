using Grand.Business.Core.Interfaces.Common.Audit;
using Grand.Data;
using Grand.Data.Audit;
using Grand.Domain;
using Grand.Domain.Audit;
using Grand.Infrastructure.Audit;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Mediator;
using System.Runtime.CompilerServices;

namespace Grand.Business.Common.Services.Audit;

public class AuditService(
    IRepository<AuditEntry> repository,
    IAuditEntryWriter writer,
    IMediator mediator,
    ICacheBase cacheBase) : IAuditService
{
    private const int StreamPageSize = 1000;

    //a distinct over the whole log; a type that appears meanwhile shows up in the filter a little later
    private const int EntityTypesCacheMinutes = 10;

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
            SearchText = search.SearchText,
            Source = search.Source,
            Kind = search.Kind,
            EntityType = search.EntityType,
            EntityId = search.EntityId,
            EntityNames = search.EntityNames,
            StoreId = search.StoreId,
            OperationId = search.OperationId
        };

        //each page continues below the time of the previous one (on the CreatedOnUtc_Id index), skipping only the
        //entries of that same time already yielded - one flush stamps many entries alike - so no page counts the
        //whole result or skips everything before it
        var skip = 0;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var page = await repository.ToListAsync(Query(bounded).Skip(skip).Take(StreamPageSize),
                cancellationToken);
            foreach (var entry in page)
                yield return entry;

            if (page.Count < StreamPageSize)
                yield break;

            var last = page[^1].CreatedOnUtc;
            skip = (bounded.ToUtc == last ? skip : 0) + page.Count(e => e.CreatedOnUtc == last);
            bounded.ToUtc = last;
        }
    }

    public Task<IList<string>> GetEntityTypes()
    {
        return cacheBase.GetAsync(CacheKey.AUDIT_ENTITY_TYPES_KEY, async () =>
        {
            var types = await repository.ToListAsync(repository.Table.Select(x => x.EntityType).Distinct());
            return (IList<string>)types.Where(t => !string.IsNullOrEmpty(t)).OrderBy(t => t, StringComparer.Ordinal)
                .ToList();
        }, EntityTypesCacheMinutes);
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
        if (!string.IsNullOrEmpty(search.SearchText))
        {
            var text = search.SearchText.ToLowerInvariant();
            query = query.Where(x => (x.ActorEmail != null && x.ActorEmail.ToLower().Contains(text))
                                     || (x.EntityName != null && x.EntityName.ToLower().Contains(text)));
        }

        if (search.Source.HasValue)
            query = query.Where(x => x.Source == search.Source.Value);
        if (search.Kind.HasValue)
            query = query.Where(x => x.Kind == search.Kind.Value);
        var types = search.EntityType?.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .ToList() ?? [];
        //the history of one record also holds the records that belong to it (its notes)
        if (types.Count == 1 && !string.IsNullOrEmpty(search.EntityId))
            query = query.Where(x => (x.EntityType == types[0] && x.EntityId == search.EntityId)
                                     || (x.OwnerType == types[0] && x.OwnerId == search.EntityId));
        else
        {
            if (types.Count == 1)
                query = query.Where(x => x.EntityType == types[0]);
            else if (types.Count > 1)
                query = query.Where(x => types.Contains(x.EntityType));
            if (!string.IsNullOrEmpty(search.EntityId))
                query = query.Where(x => x.EntityId == search.EntityId);
        }
        if (search.EntityNames is { Count: > 0 })
        {
            var names = search.EntityNames.ToList();
            query = query.Where(x => names.Contains(x.EntityName));
        }

        if (!string.IsNullOrEmpty(search.StoreId))
            query = query.Where(x => x.StoreId == search.StoreId || x.EntityStoreId == search.StoreId);
        if (!string.IsNullOrEmpty(search.OperationId))
            query = query.Where(x => x.OperationId == search.OperationId);

        return query.OrderByDescending(x => x.CreatedOnUtc).ThenByDescending(x => x.Id);
    }
}
