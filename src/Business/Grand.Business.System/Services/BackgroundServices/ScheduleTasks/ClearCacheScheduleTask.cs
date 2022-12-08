﻿using Grand.Business.Core.Interfaces.System.ScheduleTasks;
using Grand.Infrastructure.Caching;

namespace Grand.Business.System.Services.BackgroundServices.ScheduleTasks
{
    /// <summary>
    /// Clear cache scheduled task implementation
    /// </summary>
    public class ClearCacheScheduleTask : IScheduleTask
    {
        private readonly ICacheBase _cacheBase;

        public ClearCacheScheduleTask(ICacheBase cacheBase)
        {
            _cacheBase = cacheBase;
        }

        /// <summary>
        /// Executes a task
        /// </summary>
        public async Task Execute()
        {
            await _cacheBase.Clear();
        }
    }
}
