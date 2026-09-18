namespace Grand.Web.Admin.Models.Cms;

/// <summary>
///     The welcome card at the top of the admin dashboard, read from the AdminPortalInfo page.
/// </summary>
/// <param name="PageId">Page identifier, for the edit link</param>
/// <param name="Title">Title in the working language</param>
/// <param name="Body">Body in the working language; null when it cannot be rendered safely</param>
/// <param name="CanEdit">Whether the current user may edit landing pages</param>
public record AdminPortalModel(string PageId, string Title, string Body, bool CanEdit);
