﻿using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Grand.Web.Common.Binders;
using Grand.Web.Common.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Grand.Web.Models.Common
{
    public class AddressModel : BaseEntityModel
    {
        public AddressModel()
        {
            AvailableCountries = new List<SelectListItem>();
            AvailableStates = new List<SelectListItem>();
            CustomAddressAttributes = new List<AddressAttributeModel>();
        }

        public bool NameEnabled { get; set; }

        [GrandResourceDisplayName("Address.Fields.AddressName")]
        public string Name { get; set; }

        [GrandResourceDisplayName("Address.Fields.FirstName")]
        public string FirstName { get; set; }

        [GrandResourceDisplayName("Address.Fields.LastName")]
        public string LastName { get; set; }
        [DataType(DataType.EmailAddress)]
        [GrandResourceDisplayName("Address.Fields.Email")]
        public string Email { get; set; }

        public bool CompanyEnabled { get; set; }
        public bool CompanyRequired { get; set; }
        [GrandResourceDisplayName("Address.Fields.Company")]
        public string Company { get; set; }

        public bool VatNumberEnabled { get; set; }
        public bool VatNumberRequired { get; set; }
        [GrandResourceDisplayName("Address.Fields.VatNumber")]
        public string VatNumber { get; set; }

        public bool CountryEnabled { get; set; }
        [GrandResourceDisplayName("Address.Fields.Country")]
        public string CountryId { get; set; }
        [GrandResourceDisplayName("Address.Fields.Country")]
        public string CountryName { get; set; }

        public bool StateProvinceEnabled { get; set; }
        [GrandResourceDisplayName("Address.Fields.StateProvince")]
        public string StateProvinceId { get; set; }
        [GrandResourceDisplayName("Address.Fields.StateProvince")]
        public string StateProvinceName { get; set; }

        public bool CityEnabled { get; set; }
        public bool CityRequired { get; set; }
        [GrandResourceDisplayName("Address.Fields.City")]
        public string City { get; set; }

        public bool StreetAddressEnabled { get; set; }
        public bool StreetAddressRequired { get; set; }
        [GrandResourceDisplayName("Address.Fields.Address1")]
        public string Address1 { get; set; }

        public bool StreetAddress2Enabled { get; set; }
        public bool StreetAddress2Required { get; set; }
        [GrandResourceDisplayName("Address.Fields.Address2")]
        public string Address2 { get; set; }

        public bool ZipPostalCodeEnabled { get; set; }
        public bool ZipPostalCodeRequired { get; set; }
        [GrandResourceDisplayName("Address.Fields.ZipPostalCode")]
        public string ZipPostalCode { get; set; }

        public bool PhoneEnabled { get; set; }
        public bool PhoneRequired { get; set; }
        [DataType(DataType.PhoneNumber)]
        [GrandResourceDisplayName("Address.Fields.PhoneNumber")]
        public string PhoneNumber { get; set; }

        public bool FaxEnabled { get; set; }
        public bool FaxRequired { get; set; }

        [GrandResourceDisplayName("Address.Fields.FaxNumber")]
        public string FaxNumber { get; set; }

        public bool NoteEnabled { get; set; }
        [GrandResourceDisplayName("Address.Fields.Note")]
        public string Note { get; set; }

        public IList<SelectListItem> AvailableCountries { get; set; }
        public IList<SelectListItem> AvailableStates { get; set; }


        public string FormattedCustomAddressAttributes { get; set; }
        public IList<AddressAttributeModel> CustomAddressAttributes { get; set; }

        [ModelBinder(BinderType = typeof(CustomAttributesBinder))]
        public IList<CustomAttributeModel> SelectedAttributes { get; set; }
        
        public bool AddressTypeEnabled { get; set; }

        [GrandResourceDisplayName("Address.Fields.AddressType")]
        public int AddressTypeId { get; set; }

        public bool HideAddressType { get; set; }

        public bool DisallowUsersToChangeEmail { get; set; }
    }
}