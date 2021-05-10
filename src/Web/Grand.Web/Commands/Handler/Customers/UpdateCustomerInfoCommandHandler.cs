using Grand.Business.Authentication.Interfaces;
using Grand.Business.Catalog.Interfaces.Tax;
using Grand.Business.Common.Extensions;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Customers.Interfaces;
using Grand.Business.Marketing.Interfaces.Newsletters;
using Grand.Domain.Customers;
using Grand.Domain.Messages;
using Grand.Domain.Tax;
using Grand.Web.Commands.Models.Customers;
using Grand.Web.Events;
using MediatR;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Web.Commands.Handler.Customers
{
    public class UpdateCustomerInfoCommandHandler : IRequestHandler<UpdateCustomerInfoCommand, bool>
    {
        private readonly ICustomerManagerService _customerManagerService;
        private readonly IGrandAuthenticationService _authenticationService;
        private readonly IUserFieldService _userFieldService;
        private readonly IVatService _checkVatService;
        private readonly INewsLetterSubscriptionService _newsLetterSubscriptionService;
        private readonly ICustomerService _customerService;
        private readonly IMediator _mediator;

        private readonly CustomerSettings _customerSettings;
        private readonly TaxSettings _taxSettings;

        public UpdateCustomerInfoCommandHandler(
            ICustomerManagerService customerManagerService,
            IGrandAuthenticationService authenticationService,
            IUserFieldService userFieldService,
            IVatService checkVatService,
            INewsLetterSubscriptionService newsLetterSubscriptionService,
            ICustomerService customerService,
            IMediator mediator,
            CustomerSettings customerSettings,
            TaxSettings taxSettings)
        {
            _customerManagerService = customerManagerService;
            _authenticationService = authenticationService;
            _userFieldService = userFieldService;
            _checkVatService = checkVatService;
            _newsLetterSubscriptionService = newsLetterSubscriptionService;
            _customerService = customerService;
            _mediator = mediator;
            _customerSettings = customerSettings;
            _taxSettings = taxSettings;
        }

        public async Task<bool> Handle(UpdateCustomerInfoCommand request, CancellationToken cancellationToken)
        {
            //username 
            if (_customerSettings.UsernamesEnabled && _customerSettings.AllowUsersToChangeUsernames)
            {
                if (!request.Customer.Username.Equals(request.Model.Username.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    //change username
                    await _customerManagerService.SetUsername(request.Customer, request.Model.Username.Trim());
                    //re-authenticate
                    if (request.OriginalCustomerIfImpersonated == null)
                        await _authenticationService.SignIn(request.Customer, true);
                }
            }
            //email
            if (!request.Customer.Email.Equals(request.Model.Email.Trim(), StringComparison.OrdinalIgnoreCase) && _customerSettings.AllowUsersToChangeEmail)
            {
                //change email
                await _customerManagerService.SetEmail(request.Customer, request.Model.Email.Trim());
                //re-authenticate (if usernames are disabled)
                //do not authenticate users in impersonation mode
                if (request.OriginalCustomerIfImpersonated == null)
                {
                    //re-authenticate (if usernames are disabled)
                    if (!_customerSettings.UsernamesEnabled)
                        await _authenticationService.SignIn(request.Customer, true);
                }
            }

            //VAT number
            if (_taxSettings.EuVatEnabled)
            {
                await UpdateTax(request);
            }

            //form fields
            await UpdateUserFieldFields(request);

            //newsletter
            if (_customerSettings.NewsletterEnabled)
            {
                await UpdateNewsletter(request);
            }

            //save customer attributes
            await _customerService.UpdateCustomerField(request.Customer, x => x.Attributes, request.CustomerAttributes);

            //notification
            await _mediator.Publish(new CustomerInfoEvent(request.Customer, request.Model, request.Form, request.CustomerAttributes));

            return true;

        }

        private async Task UpdateTax(UpdateCustomerInfoCommand request)
        {
            var prevVatNumber = await request.Customer.GetUserField<string>(_userFieldService, SystemCustomerFieldNames.VatNumber);

            await _userFieldService.SaveField(request.Customer, SystemCustomerFieldNames.VatNumber, request.Model.VatNumber);

            if (prevVatNumber != request.Model.VatNumber)
            {
                var vat = (await _checkVatService.GetVatNumberStatus(request.Model.VatNumber));
                await _userFieldService.SaveField(request.Customer,
                        SystemCustomerFieldNames.VatNumberStatusId,
                        (int)vat.status);
            }
        }

        private async Task UpdateUserFieldFields(UpdateCustomerInfoCommand request)
        {
            if (_customerSettings.GenderEnabled)
                await _userFieldService.SaveField(request.Customer, SystemCustomerFieldNames.Gender, request.Model.Gender);

            await _userFieldService.SaveField(request.Customer, SystemCustomerFieldNames.FirstName, request.Model.FirstName);
            await _userFieldService.SaveField(request.Customer, SystemCustomerFieldNames.LastName, request.Model.LastName);
            if (_customerSettings.DateOfBirthEnabled)
            {
                DateTime? dateOfBirth = request.Model.ParseDateOfBirth();
                await _userFieldService.SaveField(request.Customer, SystemCustomerFieldNames.DateOfBirth, dateOfBirth);
            }
            if (_customerSettings.CompanyEnabled)
                await _userFieldService.SaveField(request.Customer, SystemCustomerFieldNames.Company, request.Model.Company);
            if (_customerSettings.StreetAddressEnabled)
                await _userFieldService.SaveField(request.Customer, SystemCustomerFieldNames.StreetAddress, request.Model.StreetAddress);
            if (_customerSettings.StreetAddress2Enabled)
                await _userFieldService.SaveField(request.Customer, SystemCustomerFieldNames.StreetAddress2, request.Model.StreetAddress2);
            if (_customerSettings.ZipPostalCodeEnabled)
                await _userFieldService.SaveField(request.Customer, SystemCustomerFieldNames.ZipPostalCode, request.Model.ZipPostalCode);
            if (_customerSettings.CityEnabled)
                await _userFieldService.SaveField(request.Customer, SystemCustomerFieldNames.City, request.Model.City);
            if (_customerSettings.CountryEnabled)
                await _userFieldService.SaveField(request.Customer, SystemCustomerFieldNames.CountryId, request.Model.CountryId);
            if (_customerSettings.CountryEnabled && _customerSettings.StateProvinceEnabled)
                await _userFieldService.SaveField(request.Customer, SystemCustomerFieldNames.StateProvinceId, request.Model.StateProvinceId);
            if (_customerSettings.PhoneEnabled)
                await _userFieldService.SaveField(request.Customer, SystemCustomerFieldNames.Phone, request.Model.Phone);
            if (_customerSettings.FaxEnabled)
                await _userFieldService.SaveField(request.Customer, SystemCustomerFieldNames.Fax, request.Model.Fax);
        }

        private async Task UpdateNewsletter(UpdateCustomerInfoCommand request)
        {
            var categories = new List<string>();
            foreach (string formKey in request.Form.Keys)
            {
                if (formKey.Contains("customernewsletterCategory_"))
                {
                    try
                    {
                        var category = formKey.Split('_')[1];
                        categories.Add(category);
                    }
                    catch { }
                }
            }
            //save newsletter value
            var newsletter = await _newsLetterSubscriptionService.GetNewsLetterSubscriptionByEmailAndStoreId(request.Customer.Email, request.Store.Id);

            if (newsletter != null)
            {
                newsletter.Categories.Clear();
                categories.ForEach(x => newsletter.Categories.Add(x));

                if (request.Model.Newsletter)
                {
                    newsletter.Active = true;
                    await _newsLetterSubscriptionService.UpdateNewsLetterSubscription(newsletter);
                }
                else
                {
                    newsletter.Active = false;
                    await _newsLetterSubscriptionService.UpdateNewsLetterSubscription(newsletter);
                }
            }
            else
            {
                if (request.Model.Newsletter)
                {
                    var newsLetterSubscription = new NewsLetterSubscription
                    {
                        NewsLetterSubscriptionGuid = Guid.NewGuid(),
                        Email = request.Customer.Email,
                        CustomerId = request.Customer.Id,
                        Active = true,
                        StoreId = request.Store.Id,
                        CreatedOnUtc = DateTime.UtcNow
                    };
                    categories.ForEach(x => newsLetterSubscription.Categories.Add(x));
                    await _newsLetterSubscriptionService.InsertNewsLetterSubscription(newsLetterSubscription);
                }
            }
        }
    }
}
