using Grand.Domain;

namespace Grand.Data.Audit;

/// <summary>
///     How the tracker reads documents of one type back, supplied by the repository
/// </summary>
public sealed class AuditRepositoryAccess<T>(Func<IReadOnlyCollection<string>, Task<IList<T>>> loadMany)
    where T : BaseEntity
{
    public Func<IReadOnlyCollection<string>, Task<IList<T>>> LoadMany { get; } = loadMany;
}
