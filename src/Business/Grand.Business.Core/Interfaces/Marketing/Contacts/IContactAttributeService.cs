using Grand.Domain.Messages;

namespace Grand.Business.Core.Interfaces.Marketing.Contacts
{
    /// <summary>
    /// Contact attribute service
    /// </summary>
    public interface IContactAttributeService
    {
        #region Contact attributes

        /// <summary>
        /// Deletes a contact attribute
        /// </summary>
        /// <param name="contactAttribute">Contact attribute</param>
        Task DeleteContactAttribute(ContactAttribute contactAttribute);

        /// <summary>
        /// Gets all Contact attributes
        /// </summary>
        /// <param name="storeId">Store identifier</param>
        /// <param name="ignoreAcl">Ignore acl</param>
        /// <returns>Contact attributes</returns>
        Task<IList<ContactAttribute>> GetAllContactAttributes(string storeId = "", bool ignoreAcl = false);

        /// <summary>
        /// Gets a Contact attribute 
        /// </summary>
        /// <param name="contactAttributeId">Contact attribute identifier</param>
        /// <returns>Contact attribute</returns>
        Task<ContactAttribute> GetContactAttributeById(string contactAttributeId);

        /// <summary>
        /// Inserts a Contact attribute
        /// </summary>
        /// <param name="contactAttribute">Contact attribute</param>
        Task InsertContactAttribute(ContactAttribute contactAttribute);

        /// <summary>
        /// Updates the Contact attribute
        /// </summary>
        /// <param name="contactAttribute">Contact attribute</param>
        Task UpdateContactAttribute(ContactAttribute contactAttribute);

        #endregion
    }
}
