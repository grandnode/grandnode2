﻿using Grand.Business.Core.Interfaces.Catalog.Tax;
using Grand.Business.Core.Utilities.Catalog;
using Grand.Domain.Tax;
using System.Text.RegularExpressions;

namespace Grand.Business.Catalog.Services.Tax
{
    public class VatService : IVatService
    {
        private readonly TaxSettings _taxSettings;

        public VatService(TaxSettings taxSettings)
        {
            _taxSettings = taxSettings;
        }

        /// <summary>
        /// Gets VAT Number status
        /// </summary>
        /// <param name="fullVatNumber">Two letter ISO code of a country and VAT number (e.g. GB 111 1111 111)</param>
        /// <returns>VAT Number status</returns>
        public virtual async Task<(VatNumberStatus status, string name, string address, Exception exception)> GetVatNumberStatus(string fullVatNumber)
        {
            var name = string.Empty;
            var address = string.Empty;

            if (string.IsNullOrWhiteSpace(fullVatNumber))
                return (VatNumberStatus.Empty, name, address, null);

            fullVatNumber = fullVatNumber.Trim();

            //PL 111 1111 111 or PL 1111111111
            //more advanced regex - http://codeigniter.com/wiki/European_Vat_Checker
            var r = new Regex(@"^(\w{2})(.*)");
            var match = r.Match(fullVatNumber);
            if (!match.Success)
                return (VatNumberStatus.Invalid, name, address, null);
            var twoLetterIsoCode = match.Groups[1].Value;
            var vatNumber = match.Groups[2].Value;

            return await GetVatNumberStatus(twoLetterIsoCode, vatNumber);
        }


        /// <summary>
        /// Gets VAT Number status
        /// </summary>
        /// <param name="twoLetterIsoCode">Two letter ISO code of a country</param>
        /// <param name="vatNumber">VAT number</param>
        /// <returns>VAT Number status</returns>
        public virtual async Task<(VatNumberStatus status, string name, string address, Exception exception)> GetVatNumberStatus(string twoLetterIsoCode, string vatNumber)
        {
            var name = string.Empty;
            var address = string.Empty;

            if (string.IsNullOrEmpty(twoLetterIsoCode))
                return (VatNumberStatus.Empty, name, address, null);

            if (string.IsNullOrEmpty(vatNumber))
                return (VatNumberStatus.Empty, name, address, null);

            if (_taxSettings.EuVatAssumeValid)
                return (VatNumberStatus.Valid, name, address, null);

            if (!_taxSettings.EuVatUseWebService)
                return (VatNumberStatus.Unknown, name, address, null);

            return await DoVatCheck(twoLetterIsoCode, vatNumber);
        }

        /// <summary>
        /// Performs a basic check of a VAT number for validity
        /// </summary>
        /// <param name="twoLetterIsoCode">Two letter ISO code of a country</param>
        /// <param name="vatNumber">VAT number</param>
        /// <returns>VAT number status</returns>
        public virtual async Task<(VatNumberStatus status, string name, string address, Exception exception)>
            DoVatCheck(string twoLetterIsoCode, string vatNumber)
        {
            vatNumber ??= string.Empty;
            vatNumber = vatNumber.Trim().Replace(" ", "");

            twoLetterIsoCode ??= string.Empty;
            if (!string.IsNullOrEmpty(twoLetterIsoCode))
                //The service returns INVALID_INPUT for country codes that are not uppercase.
                twoLetterIsoCode = twoLetterIsoCode.ToUpper();

            try
            {
                var result = await CheckVatRequest(new VatRequest {
                    CountryCode = twoLetterIsoCode,
                    VatNumber = vatNumber
                });
                var valid = result.Valid;
                var name = result.Name;
                var address = result.Address;

                return (valid ? VatNumberStatus.Valid : VatNumberStatus.Invalid, name, address, null);
            }
            catch (Exception ex)
            {
                return (VatNumberStatus.Unknown, string.Empty, string.Empty, ex);
            }
        }

        public Task<VatResponse> CheckVatRequest(VatRequest checkVatRequest)
        {
            return Task.FromResult(new VatResponse());
        }
    }
}
