using Grand.Domain;
using Grand.Domain.Messages;

namespace Grand.Business.Core.Interfaces.Messages;

/// <summary>
///     Message template service
/// </summary>
public interface IMessageTemplateService
{
    /// <summary>
    ///     Inserts a message template
    /// </summary>
    /// <param name="messageTemplate">Message template</param>
    Task InsertMessageTemplate(MessageTemplate messageTemplate);

    /// <summary>
    ///     Updates a message template
    /// </summary>
    /// <param name="messageTemplate">Message template</param>
    Task UpdateMessageTemplate(MessageTemplate messageTemplate);

    /// <summary>
    ///     Delete a message template
    /// </summary>
    /// <param name="messageTemplate">Message template</param>
    Task DeleteMessageTemplate(MessageTemplate messageTemplate);

    /// <summary>
    ///     Gets a message template by identifier
    /// </summary>
    /// <param name="messageTemplateId">Message template identifier</param>
    /// <returns>Message template</returns>
    Task<MessageTemplate> GetMessageTemplateById(string messageTemplateId);

    /// <summary>
    ///     Gets a message template by name
    /// </summary>
    /// <param name="messageTemplateName">Message template name</param>
    /// <param name="storeId">Store identifier</param>
    /// <returns>Message template</returns>
    Task<MessageTemplate> GetMessageTemplateByName(string messageTemplateName, string storeId);

    /// <summary>
    ///     Gets all message templates
    /// </summary>
    /// <param name="storeId">Store identifier; pass "" to load all records</param>
    /// <param name="keywords">Keywords to filter by name or subject; pass "" to skip</param>
    /// <param name="pageIndex">Page index (0-based)</param>
    /// <param name="pageSize">Page size</param>
    /// <returns>Paged list of message templates</returns>
    Task<IPagedList<MessageTemplate>> GetAllMessageTemplates(string storeId, string keywords = "", int pageIndex = 0, int pageSize = int.MaxValue);

    /// <summary>
    ///     Create a copy of message template with all depended data
    /// </summary>
    /// <param name="messageTemplate">Message template</param>
    /// <returns>Message template copy</returns>
    Task<MessageTemplate> CopyMessageTemplate(MessageTemplate messageTemplate);
}