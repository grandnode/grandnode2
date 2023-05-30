namespace Grand.Web.Admin.Models.Permissions;

public record PermissionActionSaveModel(string SystemName, string CustomerGroupId, IList<string> SelectedActions);