using Grand.Domain;
using Grand.Domain.History;

namespace Grand.Business.Core.Interfaces.Common.Directory
{
    /// <summary>
    /// History service interface
    /// </summary>
    public partial interface IHistoryService
    {
        Task SaveObject<T>(T entity) where T : BaseEntity;
        Task<IList<T>> GetHistoryForEntity<T>(BaseEntity entity) where T : BaseEntity;
        Task<IList<HistoryObject>> GetHistoryObjectForEntity(BaseEntity entity);
    }
}