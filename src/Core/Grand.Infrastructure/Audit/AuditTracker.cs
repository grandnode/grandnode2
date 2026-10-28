using Grand.Data.Audit;
using Grand.Domain;
using Grand.Domain.Audit;
using Grand.Mediator;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;

namespace Grand.Infrastructure.Audit;

/// <summary>
///     Scoped per request: snapshots each entity once before its first write, reads the "after" state in batches
///     and writes the entries in one insert per batch. Does nothing until activated for a staff request.
/// </summary>
public class AuditTracker(IAuditEntryWriter writer, IMediator mediator, ILogger<AuditTracker> logger)
    : IAuditTracker
{
    public const int BatchSize = 200;

    private const string InsertedAndDeleted = "inserted and deleted in the same operation";

    private static readonly HashSet<string> ExcludedTypes = [nameof(AuditEntry), "QueuedEmail"];

    //bookkeeping written on every save or page view; Ticks is the product's concurrency stamp
    private static readonly HashSet<string> IgnoredPaths = [
        "UpdatedOnUtc", "UpdatedBy", "CreatedOnUtc", "CreatedBy", "Ticks",
        "LastActivityDateUtc", "LastIpAddress", "LastVisitedPage"
    ];

    private readonly List<AuditEntry> _bulk = [];

    private readonly Dictionary<Type, Func<IReadOnlyCollection<string>, Task<IDictionary<string, BsonDocument>>>>
        _loaders = new();

    private readonly object _lock = new();
    private readonly Dictionary<(Type Type, string Id), Pending> _pending = new();

    //set in the flow of the AuditEntryCreated handlers: what a handler writes (e.g. a forwarding outbox) is not a
    //staff change, and recording it would publish again
    private readonly AsyncLocal<bool> _publishing = new();
    private AuditActor _actor;

    public bool IsActive => _actor != null && !_publishing.Value;

    public void Activate(AuditActor actor)
    {
        _actor = actor;
    }

    public bool IsAudited(Type entityType)
    {
        return !ExcludedTypes.Contains(entityType.Name);
    }

    public async Task BeforeWriteAsync<T>(string id, bool isInsert, AuditRepositoryAccess<T> access,
        IReadOnlyCollection<string> writtenPaths = null) where T : BaseEntity
    {
        if (!IsActive || string.IsNullOrEmpty(id))
            return;
        if (writtenPaths is { Count: > 0 } && writtenPaths.All(IgnoredPaths.Contains))
            return;

        var key = (typeof(T), id);
        bool flush;
        lock (_lock)
        {
            if (_pending.TryGetValue(key, out var existing))
            {
                existing.Merge(writtenPaths);
                return;
            }

            flush = _pending.Count >= BatchSize;
        }

        if (flush)
            await FlushAsync();

        BsonDocument before = null;
        if (!isInsert)
            try
            {
                before = (await access.LoadMany([id])).FirstOrDefault()?.ToBsonDocument();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Audit could not read {EntityType} {EntityId} before operation {OperationId}",
                    typeof(T).Name, id, _actor.OperationId);
                return;
            }

        lock (_lock)
        {
            _loaders[typeof(T)] = async ids =>
                (await access.LoadMany(ids)).ToDictionary(e => e.Id, e => e.ToBsonDocument());

            if (_pending.TryGetValue(key, out var existing))
                existing.Merge(writtenPaths);
            else
                _pending[key] = new Pending(before, writtenPaths);
        }
    }

    public void AfterFullUpdate<T>(T entity) where T : BaseEntity
    {
        if (!IsActive || entity == null)
            return;

        lock (_lock)
        {
            if (_pending.TryGetValue((typeof(T), entity.Id), out var pending))
                pending.AfterHint = entity.ToBsonDocument();
        }
    }

    public void RecordBulk<T>(string operation, string filter) where T : BaseEntity
    {
        if (!IsActive)
            return;

        lock (_lock)
        {
            _bulk.Add(new AuditEntry {
                Kind = AuditKind.Bulk,
                EntityType = typeof(T).Name,
                Details = $"{operation}: {filter}"
            });
        }
    }

    public async Task CompleteAsync()
    {
        await FlushAsync();
        //anything written after the request scope would wait in _pending for a flush that never comes
        _actor = null;
    }

    public async Task FlushAsync()
    {
        if (_actor == null)
            return;

        List<KeyValuePair<(Type Type, string Id), Pending>> pending;
        List<AuditEntry> bulk;
        Dictionary<Type, Func<IReadOnlyCollection<string>, Task<IDictionary<string, BsonDocument>>>> loaders;
        lock (_lock)
        {
            if (_pending.Count == 0 && _bulk.Count == 0)
                return;

            pending = _pending.ToList();
            bulk = _bulk.ToList();
            loaders = new Dictionary<Type, Func<IReadOnlyCollection<string>, Task<IDictionary<string, BsonDocument>>>>(_loaders);
            _pending.Clear();
            _bulk.Clear();
        }

        List<AuditEntry> entries;
        try
        {
            entries = await BuildEntries(pending, loaders);
            entries.AddRange(bulk);
            if (entries.Count == 0)
                return;

            var now = DateTime.UtcNow;
            foreach (var entry in entries)
            {
                entry.CreatedOnUtc = now;
                _actor.Stamp(entry);
            }

            await writer.InsertManyAsync(entries);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Audit entries of operation {OperationId} were not stored: {Entities}",
                _actor.OperationId, string.Join(", ", pending.Select(p => $"{p.Key.Type.Name}:{p.Key.Id}")));
            return;
        }

        _publishing.Value = true;
        foreach (var entry in entries)
            try
            {
                await mediator.Publish(new AuditEntryCreated(entry));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "A handler of AuditEntryCreated failed for entry {EntryId}", entry.Id);
            }
    }

    private static async Task<List<AuditEntry>> BuildEntries(
        List<KeyValuePair<(Type Type, string Id), Pending>> pending,
        Dictionary<Type, Func<IReadOnlyCollection<string>, Task<IDictionary<string, BsonDocument>>>> loaders)
    {
        var entries = new List<AuditEntry>();
        foreach (var group in pending.GroupBy(p => p.Key.Type))
        {
            var toRead = group.Where(p => p.Value.AfterHint == null).Select(p => p.Key.Id).ToList();
            var read = toRead.Count > 0
                ? await loaders[group.Key](toRead)
                : new Dictionary<string, BsonDocument>();

            foreach (var item in group)
            {
                var after = item.Value.AfterHint ?? (read.TryGetValue(item.Key.Id, out var stored) ? stored : null);
                AuditEntry entry;
                try
                {
                    entry = Build(group.Key.Name, item.Key.Id, item.Value, after);
                }
                catch (Exception)
                {
                    entry = new AuditEntry {
                        Kind = after == null ? AuditKind.Delete : AuditKind.Update,
                        EntityType = group.Key.Name,
                        EntityId = item.Key.Id,
                        Details = "changes could not be computed"
                    };
                }

                if (entry != null)
                    entries.Add(entry);
            }
        }

        return entries;
    }

    private static AuditEntry Build(string entityType, string entityId, Pending pending, BsonDocument after)
    {
        var before = pending.Before;
        var entry = new AuditEntry {
            EntityType = entityType,
            EntityId = entityId,
            EntityName = AuditDiff.EntityName(after ?? before),
            EntityStoreId = StoreIdOf(after ?? before)
        };

        if (after == null)
        {
            entry.Kind = AuditKind.Delete;
            if (before == null)
                entry.Details = InsertedAndDeleted;
            return entry;
        }

        var changes = AuditDiff.Compare(entityType, before, after, IgnoredPaths);
        if (before == null)
        {
            entry.Kind = AuditKind.Insert;
            entry.Changes = changes;
            return entry;
        }

        if (pending.Paths != null)
            changes = changes.Where(c => pending.Paths.Any(p => Touches(c.Path, p))).ToList();
        if (changes.Count == 0)
            return null;

        entry.Kind = AuditKind.Update;
        entry.Changes = changes;
        return entry;
    }

    private static string StoreIdOf(BsonDocument document)
    {
        return document != null && document.TryGetValue("StoreId", out var storeId) && storeId.IsString
            ? storeId.AsString
            : null;
    }

    private static bool Touches(string changePath, string writtenPath)
    {
        return changePath == writtenPath
               || changePath.StartsWith(writtenPath + ".", StringComparison.Ordinal)
               || changePath.StartsWith(writtenPath + "[", StringComparison.Ordinal);
    }

    private sealed class Pending(BsonDocument before, IReadOnlyCollection<string> paths)
    {
        public BsonDocument Before { get; } = before;

        /// <summary>
        ///     The paths written in this scope; null means every field
        /// </summary>
        public HashSet<string> Paths { get; private set; } = paths == null ? null : [..paths];

        public BsonDocument AfterHint { get; set; }

        public void Merge(IReadOnlyCollection<string> writtenPaths)
        {
            //any later write changes the stored document again, so the kept "after" is stale; a full update
            //sets it again right after its write
            AfterHint = null;
            if (writtenPaths == null)
            {
                Paths = null;
                return;
            }

            Paths?.UnionWith(writtenPaths);
        }
    }
}
