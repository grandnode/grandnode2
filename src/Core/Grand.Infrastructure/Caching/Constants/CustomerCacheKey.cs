namespace Grand.Infrastructure.Caching.Constants
{
    public static partial class CacheKey
    {
        public static string CUSTOMER_ACTION_TYPE => "Grand.customer.action.type";

        /// <summary>
        /// Key for caching
        /// </summary>
        public static string CUSTOMERATTRIBUTES_ALL_KEY => "Grand.customerattribute.all";

        /// <summary>
        /// Key for caching
        /// </summary>
        /// <remarks>
        /// {0} : customer attribute ID
        /// </remarks>
        public static string CUSTOMERATTRIBUTES_BY_ID_KEY => "Grand.customerattribute.id-{0}";

        /// <summary>
        /// Key pattern to clear cache
        /// </summary>
        public static string CUSTOMERATTRIBUTES_PATTERN_KEY => "Grand.customerattribute.";

        /// <summary>
        /// Key pattern to clear cache
        /// </summary>
        public static string CUSTOMERATTRIBUTEVALUES_PATTERN_KEY => "Grand.customerattributevalue.";

        /// <summary>
        /// Key for caching
        /// </summary>
        /// <remarks>
        /// {0} : ident
        /// </remarks>
        public static string CUSTOMERGROUPS_BY_KEY => "Grand.customergroup.key-{0}";

        /// <summary>
        /// Key for caching
        /// </summary>
        /// <remarks>
        /// </remarks>
        public static string CUSTOMERGROUPS_ALL => "Grand.customergroup.all";

        /// <summary>
        /// Key for caching
        /// </summary>
        /// <remarks>
        /// {0} : system name
        /// </remarks>
        public static string CUSTOMERGROUPS_BY_SYSTEMNAME_KEY => "Grand.customergroup.systemname-{0}";

        /// <summary>
        /// Key pattern to clear cache
        /// </summary>
        public static string CUSTOMERGROUPS_PATTERN_KEY => "Grand.customergroup.";

        /// <summary>
        /// Key for caching
        /// </summary>
        /// <remarks>
        /// {0} : customer group Id?
        /// </remarks>
        public static string CUSTOMERGROUPSPRODUCTS_ROLE_KEY => "Grand.customergroupproducts.role-{0}";

        #region Customer activity

        /// <summary>
        /// Key for caching
        /// </summary>
        /// <remarks>
        /// {0} : ident
        /// </remarks>
        public static string ACTIVITYTYPE_BY_KEY => "Grand.activitytype.id-{0}";

        /// <summary>
        /// Key for caching
        /// </summary>
        public static string ACTIVITYTYPE_ALL_KEY => "Grand.activitytype.all";

        /// <summary>
        /// Key pattern to clear cache
        /// </summary>
        public static string ACTIVITYTYPE_PATTERN_KEY => "Grand.activitytype.";

        #endregion

        #region Sales person

        /// <summary>
        /// Key for caching
        /// </summary>
        /// <remarks>
        /// {0} : salesemployee ID
        /// </remarks>
        public static string SALESEMPLOYEE_BY_ID_KEY => "Grand.salesemployee.id-{0}";

        /// <summary>
        /// Key for caching
        /// </summary>
        public static string SALESEMPLOYEE_ALL => "Grand.salesemployee.all";

        /// <summary>
        /// Key pattern to clear cache
        /// </summary>
        public static string SALESEMPLOYEE_PATTERN_KEY => "Grand.salesemployee.";

        #endregion
    }
}
