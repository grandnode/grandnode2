using Grand.Business.Common.Extensions;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Courses;
using Grand.Domain.Localization;
using Grand.Domain.Pages;
using Grand.Web.Models.Catalog;
using Grand.Web.Models.Common;
using Grand.Web.Models.Course;
using Grand.Web.Models.Pages;
using Grand.Web.Models.Vendors;
using Microsoft.AspNetCore.Http;
using System;
using System.Globalization;

namespace Grand.Web.Extensions
{
    public static class MappingExtensions
    {
        //category
        public static CategoryModel ToModel(this Category entity, Language language)
        {
            if (entity == null)
                return null;

            var model = new CategoryModel
            {
                Id = entity.Id,
                ParentCategoryId = entity.ParentCategoryId,
                Name = entity.GetTranslation(x => x.Name, language.Id),
                Description = entity.GetTranslation(x => x.Description, language.Id),
                BottomDescription = entity.GetTranslation(x => x.BottomDescription, language.Id),
                MetaKeywords = entity.GetTranslation(x => x.MetaKeywords, language.Id),
                MetaDescription = entity.GetTranslation(x => x.MetaDescription, language.Id),
                MetaTitle = entity.GetTranslation(x => x.MetaTitle, language.Id),
                SeName = entity.GetSeName(language.Id),
                Flag = entity.GetTranslation(x => x.Flag, language.Id),
                FlagStyle = entity.FlagStyle,
                Icon = entity.Icon,
                UserFields = entity.UserFields
            };
            return model;
        }
        //brand
        public static BrandModel ToModel(this Brand entity, Language language)
        {
            if (entity == null)
                return null;

            var model = new BrandModel
            {
                Id = entity.Id,
                Name = entity.GetTranslation(x => x.Name, language.Id),
                Description = entity.GetTranslation(x => x.Description, language.Id),
                BottomDescription = entity.GetTranslation(x => x.BottomDescription, language.Id),
                MetaKeywords = entity.GetTranslation(x => x.MetaKeywords, language.Id),
                MetaDescription = entity.GetTranslation(x => x.MetaDescription, language.Id),
                MetaTitle = entity.GetTranslation(x => x.MetaTitle, language.Id),
                SeName = entity.GetSeName(language.Id),
                Icon = entity.Icon,
                UserFields = entity.UserFields
            };
            return model;
        }
        //collection
        public static CollectionModel ToModel(this Collection entity, Language language)
        {
            if (entity == null)
                return null;

            var model = new CollectionModel
            {
                Id = entity.Id,
                Name = entity.GetTranslation(x => x.Name, language.Id),
                Description = entity.GetTranslation(x => x.Description, language.Id),
                BottomDescription = entity.GetTranslation(x => x.BottomDescription, language.Id),
                MetaKeywords = entity.GetTranslation(x => x.MetaKeywords, language.Id),
                MetaDescription = entity.GetTranslation(x => x.MetaDescription, language.Id),
                MetaTitle = entity.GetTranslation(x => x.MetaTitle, language.Id),
                SeName = entity.GetSeName(language.Id),
                Icon = entity.Icon,
                UserFields = entity.UserFields
            };
            return model;
        }

        //course
        public static CourseModel ToModel(this Course entity, Language language)
        {
            if (entity == null)
                return null;

            var model = new CourseModel
            {
                Id = entity.Id,
                Name = entity.GetTranslation(x => x.Name, language.Id),
                Description = entity.GetTranslation(x => x.Description, language.Id),
                ShortDescription = entity.GetTranslation(x => x.ShortDescription, language.Id),
                MetaKeywords = entity.GetTranslation(x => x.MetaKeywords, language.Id),
                MetaDescription = entity.GetTranslation(x => x.MetaDescription, language.Id),
                MetaTitle = entity.GetTranslation(x => x.MetaTitle, language.Id),
                SeName = entity.GetSeName(language.Id),
                UserFields = entity.UserFields
            };
            return model;
        }


        //page
        public static PageModel ToModel(this Page entity, Language language, IDateTimeService dateTimeService, string password = "")
        {
            var model = new PageModel
            {
                Id = entity.Id,
                SystemName = entity.SystemName,
                IncludeInSitemap = entity.IncludeInSitemap,
                IsPasswordProtected = entity.IsPasswordProtected,
                Password = (entity.Password == password) ? password : "",
                Title = entity.IsPasswordProtected && !(entity.Password == password) ? "" : entity.GetTranslation(x => x.Title, language.Id),
                Body = entity.IsPasswordProtected && !(entity.Password == password) ? "" : entity.GetTranslation(x => x.Body, language.Id),
                MetaKeywords = entity.GetTranslation(x => x.MetaKeywords, language.Id),
                MetaDescription = entity.GetTranslation(x => x.MetaDescription, language.Id),
                MetaTitle = entity.GetTranslation(x => x.MetaTitle, language.Id),
                SeName = entity.GetSeName(language.Id),
                PageLayoutId = entity.PageLayoutId,
                Published = entity.Published,
                StartDate = entity.StartDateUtc.HasValue ? dateTimeService.ConvertToUserTime(entity.StartDateUtc.Value) : default,
                EndDate = entity.EndDateUtc.HasValue ? dateTimeService.ConvertToUserTime(entity.EndDateUtc.Value) : default
            };
            return model;

        }

        public static Address ToEntity(this AddressModel model, bool trimFields = true)
        {
            if (model == null)
                return null;

            var entity = new Address();
            return ToEntity(model, entity, trimFields);
        }

        public static Address ToEntity(this AddressModel model, Address destination, bool trimFields = true)
        {
            if (model == null)
                return destination;

            if (trimFields)
            {
                if (model.FirstName != null)
                    model.FirstName = model.FirstName.Trim();
                if (model.LastName != null)
                    model.LastName = model.LastName.Trim();
                if (model.Email != null)
                    model.Email = model.Email.Trim();
                if (model.Company != null)
                    model.Company = model.Company.Trim();
                if (model.VatNumber != null)
                    model.VatNumber = model.VatNumber.Trim();
                if (model.City != null)
                    model.City = model.City.Trim();
                if (model.Address1 != null)
                    model.Address1 = model.Address1.Trim();
                if (model.Address2 != null)
                    model.Address2 = model.Address2.Trim();
                if (model.ZipPostalCode != null)
                    model.ZipPostalCode = model.ZipPostalCode.Trim();
                if (model.PhoneNumber != null)
                    model.PhoneNumber = model.PhoneNumber.Trim();
                if (model.FaxNumber != null)
                    model.FaxNumber = model.FaxNumber.Trim();
            }
            destination.FirstName = model.FirstName;
            destination.LastName = model.LastName;
            destination.Email = model.Email;
            destination.Company = model.Company;
            destination.VatNumber = model.VatNumber;
            destination.CountryId = !String.IsNullOrEmpty(model.CountryId) ? model.CountryId : "";
            destination.StateProvinceId = !String.IsNullOrEmpty(model.StateProvinceId) ? model.StateProvinceId : "";
            destination.City = model.City;
            destination.Address1 = model.Address1;
            destination.Address2 = model.Address2;
            destination.ZipPostalCode = model.ZipPostalCode;
            destination.PhoneNumber = model.PhoneNumber;
            destination.FaxNumber = model.FaxNumber;
            destination.Note = model.Note;
            destination.AddressType = (AddressType)model.AddressTypeId;

            return destination;
        }

        public static Address ToEntity(this VendorAddressModel model, Address destination, bool trimFields = true)
        {
            if (model == null)
                return destination;

            if (trimFields)
            {
                if (model.Company != null)
                    model.Company = model.Company.Trim();
                if (model.City != null)
                    model.City = model.City.Trim();
                if (model.Address1 != null)
                    model.Address1 = model.Address1.Trim();
                if (model.Address2 != null)
                    model.Address2 = model.Address2.Trim();
                if (model.ZipPostalCode != null)
                    model.ZipPostalCode = model.ZipPostalCode.Trim();
                if (model.PhoneNumber != null)
                    model.PhoneNumber = model.PhoneNumber.Trim();
                if (model.FaxNumber != null)
                    model.FaxNumber = model.FaxNumber.Trim();
            }
            destination.Company = model.Company;
            destination.CountryId = !String.IsNullOrEmpty(model.CountryId) ? model.CountryId : "";
            destination.StateProvinceId = !String.IsNullOrEmpty(model.StateProvinceId) ? model.StateProvinceId : "";
            destination.City = model.City;
            destination.Address1 = model.Address1;
            destination.Address2 = model.Address2;
            destination.ZipPostalCode = model.ZipPostalCode;
            destination.PhoneNumber = model.PhoneNumber;
            destination.FaxNumber = model.FaxNumber;
            destination.Note = model.Note;

            return destination;
        }

        public static void ParseReservationDates(this Product product, IFormCollection form,
            out DateTime? startDate, out DateTime? endDate)
        {
            startDate = null;
            endDate = null;

            string startControlId = string.Format("reservationDatepickerFrom_{0}", product.Id);
            string endControlId = string.Format("reservationDatepickerTo_{0}", product.Id);
            var ctrlStartDate = form[startControlId];
            var ctrlEndDate = form[endControlId];
            try
            {
                //currenly we support only this format (as in the \Views\Product\_RentalInfo.cshtml file)
                const string datePickerFormat = "MM/dd/yyyy";
                startDate = DateTime.ParseExact(ctrlStartDate, datePickerFormat, CultureInfo.InvariantCulture);
                endDate = DateTime.ParseExact(ctrlEndDate, datePickerFormat, CultureInfo.InvariantCulture);
            }
            catch
            {
            }
        }
    }
}