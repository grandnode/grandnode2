using Grand.Business.Core.Interfaces.Catalog.Tax;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Marketing.Newsletters;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Messages;
using Grand.Domain.Tax;
using Grand.Infrastructure.Configuration;
using Grand.Web.Commands.Models.Customers;
using Grand.Mediator;
using Grand.Web.Common.Security.Authentication;

namespace Grand.Web.Commands.Handler.Customers;

public class UpdateCustomerInfoCommandHandler : IRequestHandler<UpdateCustomerInfoCommand, bool>
{
    private readonly IGrandAuthenticationService _authenticationService;
    private readonly IVatService _checkVatService;
    private readonly ICustomerService _customerService;
    private readonly INewsLetterSubscriptionService _newsLetterSubscriptionService;
    private readonly CustomerSettings _customerSettings;
    private readonly TaxSettings _taxSettings;
    private readonly OidcLoginConfig _loginConfig;

    public UpdateCustomerInfoCommandHandler(
        IGrandAuthenticationService authenticationService,
        IVatService checkVatService,
        INewsLetterSubscriptionService newsLetterSubscriptionService,
        ICustomerService customerService,
        CustomerSettings customerSettings,
        TaxSettings taxSettings,
        OidcLoginConfig loginConfig)
    {
        _authenticationService = authenticationService;
        _checkVatService = checkVatService;
        _newsLetterSubscriptionService = newsLetterSubscriptionService;
        _customerService = customerService;
        _customerSettings = customerSettings;
        _taxSettings = taxSettings;
        _loginConfig = loginConfig;
    }

    public async Task<bool> Handle(UpdateCustomerInfoCommand request, CancellationToken cancellationToken)
    {
        //username - stored lowercased like every other write, or the lowercased sign-in lookup misses it
        //(a value differing only in case is the stored one, so this repairs rows older edits stored as typed)
        if (_customerSettings.UsernamesEnabled && _customerSettings.AllowUsersToChangeUsernames)
        {
            var username = request.Model.Username.Trim().ToLowerInvariant();
            if (request.Customer.Username != username)
            {
                //change username
                request.Customer.Username = username;
                await _customerService.UpdateCustomerField(request.Customer, x => x.Username, username);
                //re-authenticate
                if (request.OriginalCustomerIfImpersonated == null)
                    await _authenticationService.SignIn(request.Customer, true);
            }
        }

        //email - stored lowercased for the same reason. An address differing only in case is the same address,
        //so it is repaired even where changing it is not allowed
        var email = request.Model.Email.Trim().ToLowerInvariant();
        var sameAddress = string.Equals(request.Customer.Email, email, StringComparison.OrdinalIgnoreCase);
        if (request.Customer.Email != email &&
            (sameAddress || (_customerSettings.AllowUsersToChangeEmail && !_loginConfig.ProviderOnly)))
        {
            //change email
            request.Customer.Email = email;
            await _customerService.UpdateCustomerField(request.Customer, x => x.Email, email);
            //without usernames the username is the email (as registration sets it)
            if (!_customerSettings.UsernamesEnabled)
            {
                request.Customer.Username = email;
                await _customerService.UpdateCustomerField(request.Customer, x => x.Username, email);
            }
            //re-authenticate (if usernames are disabled)
            //do not authenticate users in impersonation mode
            if (request.OriginalCustomerIfImpersonated == null)
                //re-authenticate (if usernames are disabled)
                if (!_customerSettings.UsernamesEnabled)
                    await _authenticationService.SignIn(request.Customer, true);
        }

        //VAT number
        if (_taxSettings.EuVatEnabled) await UpdateTax(request);

        //form fields
        await UpdateUserFieldFields(request);

        //newsletter
        if (_customerSettings.NewsletterEnabled) await UpdateNewsletter(request);
        //save customer attributes
        await _customerService.UpdateCustomerField(request.Customer, x => x.Attributes, request.CustomerAttributes);
        return true;
    }

    private async Task UpdateTax(UpdateCustomerInfoCommand request)
    {
        var prevVatNumber = request.Customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.VatNumber);

        await _customerService.UpdateUserField(request.Customer, SystemCustomerFieldNames.VatNumber,
            request.Model.VatNumber);

        if (prevVatNumber != request.Model.VatNumber)
        {
            var vat = await _checkVatService.GetVatNumberStatus(request.Model.VatNumber);
            await _customerService.UpdateUserField(request.Customer,
                SystemCustomerFieldNames.VatNumberStatusId,
                (int)vat.status);
        }
    }

    private async Task UpdateUserFieldFields(UpdateCustomerInfoCommand request)
    {
        if (_customerSettings.GenderEnabled)
            await _customerService.UpdateUserField(request.Customer, SystemCustomerFieldNames.Gender, request.Model.Gender);

        await _customerService.UpdateUserField(request.Customer, SystemCustomerFieldNames.FirstName,
            request.Model.FirstName);
        await _customerService.UpdateUserField(request.Customer, SystemCustomerFieldNames.LastName, request.Model.LastName);
        if (_customerSettings.DateOfBirthEnabled)
        {
            var dateOfBirth = request.Model.ParseDateOfBirth();
            await _customerService.UpdateUserField(request.Customer, SystemCustomerFieldNames.DateOfBirth, dateOfBirth);
        }

        if (_customerSettings.CompanyEnabled)
            await _customerService.UpdateUserField(request.Customer, SystemCustomerFieldNames.Company,
                request.Model.Company);
        if (_customerSettings.StreetAddressEnabled)
            await _customerService.UpdateUserField(request.Customer, SystemCustomerFieldNames.StreetAddress,
                request.Model.StreetAddress);
        if (_customerSettings.StreetAddress2Enabled)
            await _customerService.UpdateUserField(request.Customer, SystemCustomerFieldNames.StreetAddress2,
                request.Model.StreetAddress2);
        if (_customerSettings.ZipPostalCodeEnabled)
            await _customerService.UpdateUserField(request.Customer, SystemCustomerFieldNames.ZipPostalCode,
                request.Model.ZipPostalCode);
        if (_customerSettings.CityEnabled)
            await _customerService.UpdateUserField(request.Customer, SystemCustomerFieldNames.City, request.Model.City);
        if (_customerSettings.CountryEnabled)
            await _customerService.UpdateUserField(request.Customer, SystemCustomerFieldNames.CountryId,
                request.Model.CountryId);
        if (_customerSettings.CountryEnabled && _customerSettings.StateProvinceEnabled)
            await _customerService.UpdateUserField(request.Customer, SystemCustomerFieldNames.StateProvinceId,
                request.Model.StateProvinceId);
        if (_customerSettings.PhoneEnabled)
            await _customerService.UpdateUserField(request.Customer, SystemCustomerFieldNames.Phone, request.Model.Phone);
        if (_customerSettings.FaxEnabled)
            await _customerService.UpdateUserField(request.Customer, SystemCustomerFieldNames.Fax, request.Model.Fax);
    }

    private async Task UpdateNewsletter(UpdateCustomerInfoCommand request)
    {
        var categories = request.Model.SelectedNewsletterCategory?.ToList();
        //save newsletter value
        var newsletter =
            await _newsLetterSubscriptionService.GetNewsLetterSubscriptionByEmailAndStoreId(request.Customer.Email,
                request.Store.Id);

        if (newsletter != null)
        {
            newsletter.Categories.Clear();
            categories?.ForEach(x => newsletter.Categories.Add(x));

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
                var newsLetterSubscription = new NewsLetterSubscription {
                    NewsLetterSubscriptionGuid = Guid.NewGuid(),
                    Email = request.Customer.Email,
                    CustomerId = request.Customer.Id,
                    Active = true,
                    StoreId = request.Store.Id
                };
                categories?.ForEach(x => newsLetterSubscription.Categories.Add(x));
                await _newsLetterSubscriptionService.InsertNewsLetterSubscription(newsLetterSubscription);
            }
        }
    }
}