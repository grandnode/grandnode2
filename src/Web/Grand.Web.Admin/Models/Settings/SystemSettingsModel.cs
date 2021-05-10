using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;

namespace Grand.Web.Admin.Models.Settings
{
    public class SystemSettingsModel : BaseModel
    {
        [GrandResourceDisplayName("Admin.Settings.System.OrderIdent")]
        public int? OrderIdent { get; set; }

        #region SystemSettings

        [GrandResourceDisplayName("Admin.Settings.System.DeleteGuestTaskOlderThanMinutes")]
        public int DeleteGuestTaskOlderThanMinutes { get; set; }

        [GrandResourceDisplayName("Admin.Settings.System.DaysToCancelUnpaidOrder")]
        public int? DaysToCancelUnpaidOrder { get; set; }

        #endregion

        #region AdminAreaSettings

        [GrandResourceDisplayName("Admin.Settings.System.DefaultGridPageSize")]
        public int DefaultGridPageSize { get; set; }

        [GrandResourceDisplayName("Admin.Settings.System.GridPageSizes")]
        public string GridPageSizes { get; set; }

        [GrandResourceDisplayName("Admin.Settings.System.UseIsoDateTimeConverterInJson")]
        public bool UseIsoDateTimeConverterInJson { get; set; }


        #endregion

        #region Language settings

        [GrandResourceDisplayName("Admin.Settings.System.DefaultAdminLanguageId")]
        public string DefaultAdminLanguageId { get; set; }

        [GrandResourceDisplayName("Admin.Settings.System.AutomaticallyDetectLanguage")]
        public bool AutomaticallyDetectLanguage { get; set; }

        [GrandResourceDisplayName("Admin.Settings.System.IgnoreRtlPropertyForAdminArea")]
        public bool IgnoreRtlPropertyForAdminArea { get; set; }

        #endregion

        #region Others 

        [GrandResourceDisplayName("Admin.Settings.System.DocumentPageSizeSettings")]
        public int DocumentPageSizeSettings { get; set; }

        #endregion
    }
}
