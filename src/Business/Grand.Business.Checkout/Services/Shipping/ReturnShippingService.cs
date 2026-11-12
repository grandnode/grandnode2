using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Domain.Shipping;
using Grand.Infrastructure.Extensions;
using Microsoft.Extensions.Logging;

namespace Grand.Business.Checkout.Services.Shipping;

public class ReturnShippingService(
    IEnumerable<IReturnShippingProvider> returnShippingProviders,
    ISettingService settingService,
    IMerchandiseReturnService merchandiseReturnService,
    IVendorService vendorService,
    ICustomerService customerService,
    ILogger<ReturnShippingService> logger) : IReturnShippingService
{
    public async Task<IList<IReturnShippingProvider>> LoadActiveProviders(Customer customer, string storeId)
    {
        var settings = await settingService.LoadSetting<ReturnShippingProviderSettings>(storeId);
        return LoadAllProviders(customer, storeId)
            .Where(provider => settings.ActiveSystemNames.Contains(provider.SystemName,
                StringComparer.OrdinalIgnoreCase))
            .ToList();
    }

    public IReturnShippingProvider LoadProviderBySystemName(string systemName)
    {
        return returnShippingProviders.FirstOrDefault(x =>
            string.Equals(x.SystemName, systemName, StringComparison.OrdinalIgnoreCase));
    }

    public IList<IReturnShippingProvider> LoadAllProviders(Customer customer = null, string storeId = "")
    {
        return returnShippingProviders
            .Where(x => x.IsAuthenticateStore(storeId) && x.IsAuthenticateGroup(customer))
            .OrderBy(x => x.Priority)
            .ToList();
    }

    public async Task<IList<ReturnShippingOffer>> GetOffers(MerchandiseReturnSubmitRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var customer = await customerService.GetCustomerById(request.CustomerId);
        var providers = await LoadActiveProviders(customer, request.StoreId);
        if (providers.Count == 0)
            return [];

        var groups = await VendorGroups(request, customer);
        if (groups.Count == 0)
            return [];

        return (await Offers(request.StoreId, groups, providers)).Select(x => x.Offer).ToList();
    }

    public async Task<ReturnShippingResolution> Resolve(MerchandiseReturnSubmitRequest request,
        ReturnShippingChoice choice)
    {
        ArgumentNullException.ThrowIfNull(request);

        var resolution = new ReturnShippingResolution();
        var customer = await customerService.GetCustomerById(request.CustomerId);
        var providers = await LoadActiveProviders(customer, request.StoreId);
        //with no return method in the store the return is sent back as it always was
        if (providers.Count == 0 && string.IsNullOrEmpty(choice?.ProviderSystemName))
            return resolution;

        //nothing submitted: the submit refuses it before any return shipping is needed
        var groups = await VendorGroups(request, customer);
        if (groups.Count == 0)
            return resolution;

        var offers = await Offers(request.StoreId, groups, providers);
        //no method fits these items (e.g. a vendor's items with only the in-store return active): the form showed
        //none, and the return is sent back as it always was
        if (offers.Count == 0 && string.IsNullOrEmpty(choice?.ProviderSystemName))
            return resolution;

        //only what is offered now can be chosen: a posted choice of an option hidden or gone is refused
        var chosen = choice == null
            ? null
            : offers.FirstOrDefault(x =>
                string.Equals(x.Offer.ProviderSystemName, choice.ProviderSystemName,
                    StringComparison.OrdinalIgnoreCase) && x.Offer.OptionCode == choice.OptionCode);
        if (chosen == null)
        {
            resolution.Errors.Add(MerchandiseReturnErrors.SubmitReturnShippingChoose);
            return resolution;
        }

        var fees = (await settingService.LoadSetting<OrderSettings>(request.StoreId)).MerchandiseReturnFees;
        var byVendor = new Dictionary<string, MerchandiseReturnShipping>();
        for (var i = 0; i < groups.Count; i++)
        {
            var group = groups[i];
            var option = chosen.Options[i];
            ReturnShippingValidationResult validation = null;
            if (option.HasForm)
                try
                {
                    validation = await chosen.Provider.ValidateForm(group.Request, option.Code,
                        choice.Form ?? new Dictionary<string, string>());
                }
                catch (Exception ex)
                {
                    //the form could not be checked: the customer picks again rather than the submit failing
                    logger.LogError(ex, "Return shipping provider {SystemName} failed to validate the form of " +
                                        "option {OptionCode} for order {OrderId}", chosen.Provider.SystemName,
                        option.Code, request.Order.Id);
                    resolution.Errors = [MerchandiseReturnErrors.SubmitReturnShippingChoose];
                    return resolution;
                }

            validation ??= new ReturnShippingValidationResult();
            //one form for all parcels: an error of it is shown once
            foreach (var error in validation.Errors.Where(error => !resolution.Errors.Contains(error)))
                resolution.Errors.Add(error);

            //the cost comes from the provider, never from what was posted
            byVendor[group.Request.VendorId] = new MerchandiseReturnShipping {
                ProviderSystemName = chosen.Provider.SystemName,
                OptionCode = option.Code,
                OptionName = option.Name,
                Cost = MerchandiseReturnShippingExtensions.CustomerCost(validation.Cost ?? option.Cost, fees,
                    group.AnyFreeReason, group.AllWithoutRefund),
                CurrencyRate = request.Order.CurrencyRate,
                DropOffPoint = validation.DropOffPoint,
                ProviderData = validation.ProviderData ?? new Dictionary<string, string>(),
                ReturnAddress = group.Request.ReturnAddress,
                LabelStatus = option.CreatesLabel ? ReturnLabelStatus.Pending : ReturnLabelStatus.NotRequired
            };
        }

        if (resolution.Errors.Count == 0)
            resolution.ByVendor = byVendor;
        return resolution;
    }

    public async Task<Address> GetReturnAddress(string storeId, string vendorId)
    {
        //a copy: the address is kept with each return, and a change of the vendor or the settings must not reach it
        if (!string.IsNullOrEmpty(vendorId))
        {
            var vendor = await vendorService.GetVendorById(vendorId);
            if (!string.IsNullOrEmpty(vendor?.Address?.CountryId) && !string.IsNullOrEmpty(vendor.Address.Address1))
                return Copy(vendor.Address);
        }

        var settings = await settingService.LoadSetting<ReturnShippingProviderSettings>(storeId);
        if (!string.IsNullOrEmpty(settings.ReturnAddress?.Address1))
            return Copy(settings.ReturnAddress);

        return Copy((await settingService.LoadSetting<ShippingSettings>(storeId)).ShippingOriginAddress);
    }

    /// <summary>
    ///     The submitted items by vendor, grouped as the submit groups them into returns
    /// </summary>
    private async Task<IList<VendorGroup>> VendorGroups(MerchandiseReturnSubmitRequest request, Customer customer)
    {
        var items = request.Items.Where(x => x.Quantity > 0).ToList();
        var vendorItems = request.Order.OrderItems
            .Where(orderItem => items.Any(x => x.OrderItemId == orderItem.Id))
            .GroupBy(orderItem => orderItem.VendorId ?? "")
            .ToList();
        if (vendorItems.Count == 0)
            return [];

        var reasons = await merchandiseReturnService.GetAllMerchandiseReturnReasons(request.StoreId);
        var actions = await merchandiseReturnService.GetAllMerchandiseReturnActions(request.StoreId);
        var groups = new List<VendorGroup>();
        foreach (var orderItems in vendorItems)
        {
            var groupItems = items.Where(x => orderItems.Any(orderItem => orderItem.Id == x.OrderItemId)).ToList();
            groups.Add(new VendorGroup(
                new ReturnShippingRequest {
                    Order = request.Order,
                    Customer = customer,
                    StoreId = request.StoreId,
                    VendorId = orderItems.Key,
                    Items = orderItems.Select(orderItem => (orderItem,
                        groupItems.Where(x => x.OrderItemId == orderItem.Id).Sum(x => x.Quantity))).ToList(),
                    ReturnAddress = await GetReturnAddress(request.StoreId, orderItems.Key),
                    PickupAddress = request.PickupAddress,
                    LanguageId = request.LanguageId
                },
                groupItems.Any(item => reasons.Any(x => x.Id == item.ReasonId && x.FreeReturnShipping)),
                //an item with no action, or one the store does not offer, is returned for its money
                groupItems.All(item => actions.Any(x => x.Id == item.ActionId && x.WithoutRefund))));
        }

        return groups;
    }

    /// <summary>
    ///     The options every vendor group is offered, with the option of each group in the order of the groups
    /// </summary>
    private async Task<IList<ProviderOffer>> Offers(string storeId, IList<VendorGroup> groups,
        IList<IReturnShippingProvider> providers)
    {
        if (providers.Count == 0)
            return [];

        var fees = (await settingService.LoadSetting<OrderSettings>(storeId)).MerchandiseReturnFees;
        var offers = new List<ProviderOffer>();
        foreach (var provider in providers)
        {
            var groupOptions = new List<IList<ReturnShippingOption>>();
            var formRoutes = new Dictionary<string, string>();
            try
            {
                foreach (var group in groups)
                    groupOptions.Add(await provider.GetReturnOptions(group.Request) ?? []);
                //asked for the submit too: an option whose form cannot be shown is neither offered nor accepted, so
                //the form and the submit always agree on what can be chosen
                foreach (var option in groupOptions[0].Where(x => x.HasForm))
                    formRoutes[option.Code] = await provider.GetFormRouteName(option.Code);
            }
            catch (Exception ex)
            {
                //e.g. a carrier that is down: its options are not offered, the other providers' still are
                logger.LogError(ex, "Return shipping provider {SystemName} failed to give its options or their " +
                                    "forms for order {OrderId}", provider.SystemName, groups[0].Request.Order.Id);
                continue;
            }

            //one option for all parcels: an option a vendor does not offer cannot be picked for the return
            foreach (var first in groupOptions[0])
            {
                var options = groupOptions.Select(x => x.FirstOrDefault(option => option.Code == first.Code))
                    .ToList();
                if (options.Any(x => x == null))
                    continue;

                offers.Add(new ProviderOffer(provider, options, options.Sum(x => x.Cost), new ReturnShippingOffer {
                    ProviderSystemName = provider.SystemName,
                    OptionCode = first.Code,
                    Name = first.Name,
                    Description = first.Description,
                    Cost = groups.Select((group, i) => MerchandiseReturnShippingExtensions.CustomerCost(
                        options[i].Cost, fees, group.AnyFreeReason, group.AllWithoutRefund)).Sum(),
                    Parcels = groups.Count,
                    HasForm = first.HasForm,
                    FormRouteName = first.HasForm ? formRoutes.GetValueOrDefault(first.Code) : null
                }));
            }
        }

        //nothing is refunded, so the store pays the shipping: no paid option while a free one is offered
        if (groups.All(x => x.AllWithoutRefund) && offers.Any(x => x.OptionCost == 0))
            offers.RemoveAll(x => x.OptionCost > 0);

        return offers;
    }

    private static Address Copy(Address address)
    {
        if (address == null)
            return null;

        return new Address {
            Name = address.Name,
            FirstName = address.FirstName,
            LastName = address.LastName,
            Email = address.Email,
            Company = address.Company,
            VatNumber = address.VatNumber,
            CountryId = address.CountryId,
            StateProvinceId = address.StateProvinceId,
            City = address.City,
            Address1 = address.Address1,
            Address2 = address.Address2,
            ZipPostalCode = address.ZipPostalCode,
            PhoneNumber = address.PhoneNumber,
            FaxNumber = address.FaxNumber,
            Note = address.Note,
            Attributes = address.Attributes?.Select(x => new CustomAttribute { Key = x.Key, Value = x.Value })
                .ToList() ?? [],
            AddressType = address.AddressType
        };
    }

    private record VendorGroup(ReturnShippingRequest Request, bool AnyFreeReason, bool AllWithoutRefund);

    /// <summary>
    ///     An offer with what it costs before the store's return fee rules
    /// </summary>
    private record ProviderOffer(
        IReturnShippingProvider Provider,
        IList<ReturnShippingOption> Options,
        double OptionCost,
        ReturnShippingOffer Offer);
}
