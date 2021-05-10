using System.Collections.Generic;

namespace Grand.Infrastructure.Plugins
{
    public interface IProvider
    {
        /// <summary>
        /// Gets a configuration URL
        /// </summary>
        string ConfigurationUrl { get; }
        /// <summary>
        /// Gets a system name for provider 
        /// </summary>
        string SystemName { get; }
        /// <summary>
        /// Gets a friendly name for provider 
        /// </summary>
        string FriendlyName { get; }
        /// <summary>
        /// Gets a priority for provider 
        /// </summary>
        int Priority { get; }
        /// <summary>
        /// Gets a limited to store for provider 
        /// </summary>
        IList<string> LimitedToStores { get; }
        /// <summary>
        /// Gets a limited to customer group for provider 
        /// </summary>
        IList<string> LimitedToGroups { get; }
    }
}
