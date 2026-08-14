using Grand.Web.AdminShared.Models.Messages;

namespace Grand.Web.Store.Models.Messages;

public class MessageTemplateStoreModel : MessageTemplateModel
{
    /// <summary>
    /// True when the store manager can only preview the template (global, or shared with other stores),
    /// not save changes to it.
    /// </summary>
    public bool IsReadOnly { get; set; }
}
