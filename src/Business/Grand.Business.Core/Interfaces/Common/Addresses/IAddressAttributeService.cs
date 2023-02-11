using Grand.Domain.Common;

namespace Grand.Business.Core.Interfaces.Common.Addresses
{
    /// <summary>
    /// Address attribute service
    /// </summary>
    public interface IAddressAttributeService
    {
        /// <summary>
        /// Gets all address attributes
        /// </summary>
        /// <returns>Address attributes</returns>
        Task<IList<AddressAttribute>> GetAllAddressAttributes();

        /// <summary>
        /// Gets an address attribute 
        /// </summary>
        /// <param name="addressAttributeId">Address attribute identifier</param>
        /// <returns>Address attribute</returns>
        Task<AddressAttribute> GetAddressAttributeById(string addressAttributeId);

        /// <summary>
        /// Inserts an address attribute
        /// </summary>
        /// <param name="addressAttribute">Address attribute</param>
        Task InsertAddressAttribute(AddressAttribute addressAttribute);

        /// <summary>
        /// Updates the address attribute
        /// </summary>
        /// <param name="addressAttribute">Address attribute</param>
        Task UpdateAddressAttribute(AddressAttribute addressAttribute);

        /// <summary>
        /// Deletes an address attribute
        /// </summary>
        /// <param name="addressAttribute">Address attribute</param>
        Task DeleteAddressAttribute(AddressAttribute addressAttribute);
       
        /// <summary>
        /// Inserts a address attribute value
        /// </summary>
        /// <param name="addressAttributeValue">Address attribute value</param>
        Task InsertAddressAttributeValue(AddressAttributeValue addressAttributeValue);

        /// <summary>
        /// Updates the address attribute value
        /// </summary>
        /// <param name="addressAttributeValue">Address attribute value</param>
        Task UpdateAddressAttributeValue(AddressAttributeValue addressAttributeValue);
        /// <summary>
        /// Deletes an address attribute value
        /// </summary>
        /// <param name="addressAttributeValue">Address attribute value</param>
        Task DeleteAddressAttributeValue(AddressAttributeValue addressAttributeValue);

    }
}
