﻿using Grand.Business.Core.Utilities.Catalog;
using Grand.Domain.Tax;

namespace Grand.Business.Core.Interfaces.Catalog.Tax
{
    public interface IVatService
    {
        /// <summary>
        /// Gets VAT Number status
        /// </summary>
        /// <param name="fullVatNumber">Two letter ISO code of a country and VAT number (e.g. GB 111 1111 111)</param>
        /// <returns>VAT Number status</returns>
        Task<(VatNumberStatus status, string name, string address, Exception exception)> GetVatNumberStatus(string fullVatNumber);

        /// <summary>
        /// Gets VAT Number status
        /// </summary>
        /// <param name="twoLetterIsoCode">Two letter ISO code of a country</param>
        /// <param name="vatNumber">VAT number</param>
        /// <returns>VAT Number status</returns>
        Task<(VatNumberStatus status, string name, string address, Exception exception)> GetVatNumberStatus(string twoLetterIsoCode, string vatNumber);


        /// <summary>
        /// Performs a basic check of a VAT number for validity
        /// </summary>
        /// <param name="twoLetterIsoCode">Two letter ISO code of a country</param>
        /// <param name="vatNumber">VAT number</param>
        /// <returns>VAT number status</returns>
        Task<(VatNumberStatus status, string name, string address, Exception exception)> DoVatCheck(string twoLetterIsoCode, string vatNumber);

        /// <summary>
        /// Check Vat request
        /// </summary>
        /// <param name="checkVatRequest"></param>
        /// <returns>CheckVatResponse</returns>
        Task<VatResponse> CheckVatRequest(VatRequest checkVatRequest);
    }
}
