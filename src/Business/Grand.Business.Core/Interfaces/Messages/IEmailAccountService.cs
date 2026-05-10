using Grand.Domain;
using Grand.Domain.Messages;

namespace Grand.Business.Core.Interfaces.Messages;

public interface IEmailAccountService
{
    /// <summary>
    ///     Inserts an email account
    /// </summary>
    /// <param name="emailAccount">Email account</param>
    Task InsertEmailAccount(EmailAccount emailAccount);

    /// <summary>
    ///     Updates an email account
    /// </summary>
    /// <param name="emailAccount">Email account</param>
    Task UpdateEmailAccount(EmailAccount emailAccount);

    /// <summary>
    ///     Deletes an email account
    /// </summary>
    /// <param name="emailAccount">Email account</param>
    Task DeleteEmailAccount(EmailAccount emailAccount);

    /// <summary>
    ///     Gets an email account by identifier
    /// </summary>
    /// <param name="emailAccountId">The email account identifier</param>
    /// <returns>Email account</returns>
    Task<EmailAccount> GetEmailAccountById(string emailAccountId);

    /// <summary>
    ///     Gets all email accounts, optionally filtered by store, with pagination support.
    /// </summary>
    /// <param name="storeId">Store identifier; pass empty string to return all accounts</param>
    /// <param name="pageIndex">Page index (0-based)</param>
    /// <param name="pageSize">Page size</param>
    /// <returns>Paged list of email accounts</returns>
    Task<IPagedList<EmailAccount>> GetAllEmailAccounts(string storeId = "", int pageIndex = 0, int pageSize = int.MaxValue);
}