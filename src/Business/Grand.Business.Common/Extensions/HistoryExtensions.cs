using Grand.Business.Common.Interfaces.Directory;
using Grand.Domain;
using Grand.Domain.History;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Grand.Business.Common.Extensions
{
    public static class HistoryExtensions
    {
        /// <summary>
        /// Save an entity
        /// </summary>
        /// <param name="entity">Entity</param>
        public static async Task SaveHistory<T>(this BaseEntity entity, IHistoryService historyService) where T : BaseEntity, IHistory
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            await historyService.SaveObject(entity);
        }

        public static async Task<IList<HistoryObject>> GetHistoryObject(this BaseEntity entity, IHistoryService historyService)
        {
            return await historyService.GetHistoryObjectForEntity(entity);
        }
    }
}
