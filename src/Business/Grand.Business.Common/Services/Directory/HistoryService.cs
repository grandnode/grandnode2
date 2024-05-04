using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Data;
using Grand.Domain;
using Grand.Domain.History;

namespace Grand.Business.Common.Services.Directory;

/// <summary>
///     History service interface
/// </summary>
public class HistoryService : IHistoryService
{
    private readonly IRepository<HistoryObject> _historyRepository;

    public HistoryService(IRepository<HistoryObject> historyRepository)
    {
        _historyRepository = historyRepository;
    }

    public virtual async Task SaveObject<T>(T entity) where T : BaseEntity
    {
        ArgumentNullException.ThrowIfNull(entity);
        var history = new HistoryObject {
            Object = entity
        };
        await _historyRepository.InsertAsync(history);
    }

    public virtual async Task<IList<T>> GetHistoryForEntity<T>(BaseEntity entity) where T : BaseEntity
    {
        ArgumentNullException.ThrowIfNull(entity);

        var history = await Task.FromResult(_historyRepository.Table.Where(x => x.Object.Id == entity.Id)
            .Select(x => (T)x.Object).ToList());
        return history;
    }

    public virtual async Task<IList<HistoryObject>> GetHistoryObjectForEntity(BaseEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var history = await Task.FromResult(_historyRepository.Table.Where(x => x.Object.Id == entity.Id).ToList());
        return history;
    }
}