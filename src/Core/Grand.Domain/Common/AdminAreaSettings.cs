﻿using Grand.Domain.Configuration;

namespace Grand.Domain.Common
{
    public class AdminAreaSettings : ISettings
    {
        /// <summary>
        /// Default grid page size
        /// </summary>
        public int DefaultGridPageSize { get; set; }
        /// <summary>
        /// A comma-separated list of available grid page sizes
        /// </summary>
        public string GridPageSizes { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether to use IsoDateTimeConverter in Json results (used for avoiding issue with dates in KendoUI grids)
        /// </summary>
        public bool UseIsoDateTimeConverterInJson { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to hide column Store in a grid list
        /// </summary>
        public bool HideStoreColumn { get; set; }

    }
}