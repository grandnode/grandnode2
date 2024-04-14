using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using System.ComponentModel.DataAnnotations;

namespace Grand.Web.Admin.Models.Common;

public class MaintenanceModel : BaseModel
{
    public DeleteGuestsModel DeleteGuests { get; set; } = new();
    public DeleteAbandonedCartsModel DeleteAbandonedCarts { get; set; } = new();
    public DeleteExportedFilesModel DeleteExportedFiles { get; set; } = new();
    public ConvertPictureModel ConvertedPictureModel { get; set; } = new() { NumberOfConvertItems = -1 };

    #region Nested classes

    public class DeleteGuestsModel : BaseModel
    {
        [GrandResourceDisplayName("Admin.System.Maintenance.DeleteGuests.StartDate")]
        [UIHint("DateNullable")]
        public DateTime? StartDate { get; set; }

        [GrandResourceDisplayName("Admin.System.Maintenance.DeleteGuests.EndDate")]
        [UIHint("DateNullable")]
        public DateTime? EndDate { get; set; }

        [GrandResourceDisplayName("Admin.System.Maintenance.DeleteGuests.OnlyWithoutShoppingCart")]
        public bool OnlyWithoutShoppingCart { get; set; }

        public int? NumberOfDeletedCustomers { get; set; }
    }

    public class DeleteAbandonedCartsModel : BaseModel
    {
        [GrandResourceDisplayName("Admin.System.Maintenance.DeleteAbandonedCarts.OlderThan")]
        [UIHint("Date")]
        public DateTime OlderThan { get; set; }

        public int? NumberOfDeletedItems { get; set; }
    }

    public class DeleteExportedFilesModel : BaseModel
    {
        [GrandResourceDisplayName("Admin.System.Maintenance.DeleteExportedFiles.StartDate")]
        [UIHint("DateNullable")]
        public DateTime? StartDate { get; set; }

        [GrandResourceDisplayName("Admin.System.Maintenance.DeleteExportedFiles.EndDate")]
        [UIHint("DateNullable")]
        public DateTime? EndDate { get; set; }

        public int? NumberOfDeletedFiles { get; set; }
    }

    public class ConvertPictureModel : BaseModel
    {
        public int NumberOfConvertItems { get; set; }
    }

    #endregion
}