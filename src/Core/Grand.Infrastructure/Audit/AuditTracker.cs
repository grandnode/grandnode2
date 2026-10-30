using Grand.Data.Audit;
using Grand.Domain;
using Grand.Domain.Audit;
using Grand.Mediator;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace Grand.Infrastructure.Audit;

/// <summary>
///     Scoped per request: snapshots each entity once before its first write, reads the "after" state in batches
///     and writes the entries in one insert per batch. Does nothing until activated for a staff request.
/// </summary>
public class AuditTracker(
    IAuditEntryWriter writer,
    IMediator mediator,
    IAuditRecordReader recordReader,
    ILogger<AuditTracker> logger)
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
    private readonly Dictionary<Type, int> _countedInserts = new();

    private readonly Dictionary<Type, Func<IReadOnlyCollection<string>, Task<IDictionary<string, IDictionary<string, string>>>>>
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

    public async Task BeforeWriteAsync<T>(string id, bool isInsert,
        Func<IReadOnlyCollection<string>, Task<IList<T>>> loadMany, IReadOnlyCollection<string> writtenPaths = null)
        where T : BaseEntity
    {
        if (!IsActive || string.IsNullOrEmpty(id))
            return;
        if (writtenPaths is { Count: > 0 } && writtenPaths.All(IgnoredPaths.Contains))
            return;
        if (isInsert)
            lock (_lock)
            {
                if (_countedInserts.TryGetValue(typeof(T), out var counted))
                {
                    _countedInserts[typeof(T)] = counted + 1;
                    return;
                }
            }

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

        IDictionary<string, string> before = null;
        if (!isInsert)
            try
            {
                var stored = (await loadMany([id])).FirstOrDefault();
                before = stored == null ? null : AuditSnapshot.Take(stored);
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
                (await loadMany(ids)).ToDictionary(e => e.Id, e => AuditSnapshot.Take(e));

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

        var key = (typeof(T), entity.Id);
        lock (_lock)
        {
            if (!_pending.ContainsKey(key))
                return;
        }

        //runs after the write: a failure here must not turn a stored change into an error
        try
        {
            var after = AuditSnapshot.Take(entity);
            lock (_lock)
            {
                if (_pending.TryGetValue(key, out var pending))
                    pending.AfterHint = after;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Audit could not keep {EntityType} {EntityId} after operation {OperationId}",
                typeof(T).Name, entity.Id, _actor.OperationId);
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

    public IDisposable CountInserts<T>(string operation, string details, BaseEntity owner = null)
        where T : BaseEntity
    {
        if (!IsActive)
            return new Scope(null);

        lock (_lock)
        {
            _countedInserts[typeof(T)] = 0;
        }

        return new Scope(() =>
        {
            int count;
            lock (_lock)
            {
                _countedInserts.Remove(typeof(T), out count);
            }

            if (count == 0)
                return;
            lock (_lock)
            {
                _bulk.Add(new AuditEntry {
                    Kind = AuditKind.Bulk,
                    EntityType = typeof(T).Name,
                    Details = $"{operation}: {details}; inserted: {count}",
                    OwnerType = owner?.GetType().Name,
                    OwnerId = owner?.Id
                });
            }
        });
    }

    private sealed class Scope(Action end) : IDisposable
    {
        public void Dispose()
        {
            end?.Invoke();
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
        Dictionary<Type, Func<IReadOnlyCollection<string>, Task<IDictionary<string, IDictionary<string, string>>>>> loaders;
        lock (_lock)
        {
            if (_pending.Count == 0 && _bulk.Count == 0)
                return;

            pending = _pending.ToList();
            bulk = _bulk.ToList();
            loaders = new Dictionary<Type, Func<IReadOnlyCollection<string>, Task<IDictionary<string, IDictionary<string, string>>>>>(_loaders);
            _pending.Clear();
            _bulk.Clear();
        }

        List<AuditEntry> entries;
        try
        {
            entries = await BuildEntries(pending, loaders, new AuditReferences(recordReader));
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
        Dictionary<Type, Func<IReadOnlyCollection<string>, Task<IDictionary<string, IDictionary<string, string>>>>> loaders,
        AuditReferences references)
    {
        //an entry of null: its changes could not be computed
        var built = new List<(Type Type, string Id, AuditEntry Entry, IDictionary<string, string> Before,
            IDictionary<string, string> After)>();
        foreach (var group in pending.GroupBy(p => p.Key.Type))
        {
            var toRead = group.Where(p => p.Value.AfterHint == null).Select(p => p.Key.Id).ToList();
            var read = toRead.Count > 0
                ? await loaders[group.Key](toRead)
                : new Dictionary<string, IDictionary<string, string>>();

            foreach (var item in group)
            {
                var after = item.Value.AfterHint ?? (read.TryGetValue(item.Key.Id, out var stored) ? stored : null);
                try
                {
                    var entry = Build(group.Key, item.Key.Id, item.Value, after);
                    if (entry != null)
                        built.Add((group.Key, item.Key.Id, entry, item.Value.Before, after));
                }
                catch (Exception)
                {
                    built.Add((group.Key, item.Key.Id, null, item.Value.Before, after));
                }
            }
        }

        //the names of all entries in a few reads, instead of one read per referenced record
        await references.Prefetch(built.Where(b => b.Entry != null).Select(b => (b.Type, b.Entry, b.Before, b.After)));

        var entries = new List<AuditEntry>();
        foreach (var item in built)
        {
            var entry = item.Entry;
            if (entry != null)
                try
                {
                    await references.DescribeEntry(item.Type, entry, item.Before, item.After);
                }
                catch (Exception)
                {
                    entry = null;
                }

            entries.Add(entry ?? new AuditEntry {
                Kind = item.After == null ? AuditKind.Delete : AuditKind.Update,
                EntityType = item.Type.Name,
                EntityId = item.Id,
                Details = "changes could not be computed"
            });
        }

        return entries;
    }

    private static AuditEntry Build(Type type, string entityId, Pending pending,
        IDictionary<string, string> after)
    {
        var entityType = type.Name;
        var before = pending.Before;
        var entry = new AuditEntry {
            EntityType = entityType,
            EntityId = entityId,
            EntityName = NameOf(type, after ?? before),
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

    private static string NameOf(Type type, IDictionary<string, string> snapshot)
    {
        //a name can be a note or a comment: kept to the length of a stored value
        var property = AuditReferences.NameProperty(type);
        if (snapshot == null || !snapshot.TryGetValue(property, out var name) || name == "")
            return null;
        return name.Length > AuditDiff.MaxValueLength ? name[..AuditDiff.MaxValueLength] : name;
    }

    //"" is a record of all stores, null one without a store
    private static string StoreIdOf(IDictionary<string, string> snapshot)
    {
        return snapshot != null && snapshot.TryGetValue("StoreId", out var storeId) ? storeId : null;
    }

    private static bool Touches(string changePath, string writtenPath)
    {
        return changePath == writtenPath
               || changePath.StartsWith(writtenPath + ".", StringComparison.Ordinal)
               || changePath.StartsWith(writtenPath + "[", StringComparison.Ordinal);
    }

    private sealed class Pending(IDictionary<string, string> before, IReadOnlyCollection<string> paths)
    {
        public IDictionary<string, string> Before { get; } = before;

        /// <summary>
        ///     The paths written in this scope; null means every field
        /// </summary>
        public HashSet<string> Paths { get; private set; } = paths == null ? null : [..paths];

        /// <summary>
        ///     The state a full update wrote, so the flush does not re-read it
        /// </summary>
        public IDictionary<string, string> AfterHint { get; set; }

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
