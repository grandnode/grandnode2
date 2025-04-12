namespace Grand.Web.AdminShared.Models.Permissions;

public record PermissionActionSaveModel(string SystemName, string CustomerGroupId, IList<string> SelectedActions);