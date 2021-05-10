using Grand.Business.Checkout.Interfaces.Orders;
using Grand.Business.Checkout.Queries.Models.Orders;
using Grand.Business.Common.Interfaces.Addresses;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Domain.Common;
using Grand.Domain.Orders;
using Grand.Infrastructure;
using Grand.Web.Commands.Models.Orders;
using Grand.Web.Extensions;
using Grand.Web.Features.Models.Common;
using Grand.Web.Features.Models.Orders;
using Grand.Web.Models.Common;
using Grand.Web.Models.Orders;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Web.Controllers
{
    public partial class MerchandiseReturnController : BasePublicController
    {
        #region Fields

        private readonly IMerchandiseReturnService _merchandiseReturnService;
        private readonly IOrderService _orderService;
        private readonly IWorkContext _workContext;
        private readonly IGroupService _groupService;
        private readonly ITranslationService _translationService;
        private readonly IMediator _mediator;

        private readonly OrderSettings _orderSettings;
        #endregion

        #region Constructors

        public MerchandiseReturnController(
            IMerchandiseReturnService merchandiseReturnService,
            IOrderService orderService,
            IWorkContext workContext,
            IGroupService groupService,
            ITranslationService translationService,
            IMediator mediator,
            OrderSettings orderSettings)
        {
            _merchandiseReturnService = merchandiseReturnService;
            _orderService = orderService;
            _workContext = workContext;
            _groupService = groupService;
            _translationService = translationService;
            _mediator = mediator;
            _orderSettings = orderSettings;
        }

        #endregion

        #region Utilities

        private async Task PrepareModelAddress(AddressModel addressModel, Address address)
        {
            var countryService = HttpContext.RequestServices.GetRequiredService<ICountryService>();
            var countries = await countryService.GetAllCountries(_workContext.WorkingLanguage.Id, _workContext.CurrentStore.Id);
            addressModel = await _mediator.Send(new GetAddressModel()
            {
                Language = _workContext.WorkingLanguage,
                Store = _workContext.CurrentStore,
                Customer = _workContext.CurrentCustomer,
                Model = addressModel,
                Address = address,
                ExcludeProperties = true,
                PrePopulateWithCustomerFields = true,
                LoadCountries = () => countries
            });
        }

        protected async Task<Address> PrepareAddress(MerchandiseReturnModel model, IFormCollection form)
        {
            string pickupAddressId = form["pickup_address_id"];
            var address = new Address();
            if (_orderSettings.MerchandiseReturns_AllowToSpecifyPickupAddress)
            {
                if (!string.IsNullOrEmpty(pickupAddressId))
                {
                    address = _workContext.CurrentCustomer.Addresses.FirstOrDefault(a => a.Id == pickupAddressId);
                }
                else
                {
                    var customAttributes = await _mediator.Send(new GetParseCustomAddressAttributes() { Form = form });
                    var addressAttributeParser = HttpContext.RequestServices.GetRequiredService<IAddressAttributeParser>();
                    var customAttributeWarnings = await addressAttributeParser.GetAttributeWarnings(customAttributes);
                    foreach (var error in customAttributeWarnings)
                    {
                        ModelState.AddModelError("", error);
                    }
                    await TryUpdateModelAsync(model.NewAddress, "MerchandiseReturnNewAddress");
                    address = model.NewAddress.ToEntity();
                    model.NewAddressPreselected = true;
                    address.Attributes = customAttributes;
                    address.CreatedOnUtc = DateTime.UtcNow;
                }
            }
            return address;
        }


        #endregion

        #region Methods

        public virtual async Task<IActionResult> CustomerMerchandiseReturns()
        {
            if (!await _groupService.IsRegistered(_workContext.CurrentCustomer))
                return Challenge();

            var model = await _mediator.Send(new GetMerchandiseReturns()
            {
                Customer = _workContext.CurrentCustomer,
                Store = _workContext.CurrentStore,
                Language = _workContext.WorkingLanguage
            });

            return View(model);
        }

        public virtual async Task<IActionResult> MerchandiseReturn(string orderId, string errors = "")
        {
            var order = await _orderService.GetOrderById(orderId);
            if (!await order.Access(_workContext.CurrentCustomer, _groupService))
                return Challenge();

            if (!await _mediator.Send(new IsMerchandiseReturnAllowedQuery() { Order = order }))
                return RedirectToRoute("HomePage");

            var model = await _mediator.Send(new GetMerchandiseReturn()
            {
                Order = order,
                Language = _workContext.WorkingLanguage,
                Store = _workContext.CurrentStore
            });
            model.Error = errors;
            return View(model);
        }

        [HttpPost, ActionName("MerchandiseReturn")]
        [AutoValidateAntiforgeryToken]
        public virtual async Task<IActionResult> MerchandiseReturnSubmit(string orderId, MerchandiseReturnModel model, IFormCollection form)
        {
            var order = await _orderService.GetOrderById(orderId);
            if (!await order.Access(_workContext.CurrentCustomer, _groupService))
                return Challenge();

            if (!await _mediator.Send(new IsMerchandiseReturnAllowedQuery() { Order = order }))
                return RedirectToRoute("HomePage");

            ModelState.Clear();

            if (_orderSettings.MerchandiseReturns_AllowToSpecifyPickupDate && _orderSettings.MerchandiseReturns_PickupDateRequired && model.PickupDate == null)
            {
                ModelState.AddModelError("", _translationService.GetResource("MerchandiseReturns.PickupDateRequired"));
            }

            var address = await PrepareAddress(model, form);

            if (!ModelState.IsValid && ModelState.ErrorCount > 0)
            {
                var returnmodel = await _mediator.Send(new GetMerchandiseReturn()
                {
                    Order = order,
                    Language = _workContext.WorkingLanguage,
                    Store = _workContext.CurrentStore
                });
                returnmodel.Error = string.Join(", ", ModelState.Keys.SelectMany(k => ModelState[k].Errors).Select(m => m.ErrorMessage).ToArray());
                returnmodel.Comments = model.Comments;
                returnmodel.PickupDate = model.PickupDate;
                returnmodel.NewAddressPreselected = model.NewAddressPreselected;
                returnmodel.NewAddress = model.NewAddress;
                if (returnmodel.NewAddressPreselected || _orderSettings.MerchandiseReturns_AllowToSpecifyPickupAddress)
                {
                    await PrepareModelAddress(model.NewAddress, address);
                }
                return View(returnmodel);
            }
            else
            {
                var result = await _mediator.Send(new MerchandiseReturnSubmitCommand() { Address = address, Model = model, Form = form, Order = order });
                if (result.rr.ReturnNumber > 0)
                {
                    model.Result = string.Format(_translationService.GetResource("MerchandiseReturns.Submitted"), result.rr.ReturnNumber, Url.Link("MerchandiseReturnDetails", new { merchandiseReturnId = result.rr.Id }));
                    model.OrderNumber = order.OrderNumber;
                    model.OrderCode = order.Code;
                    return View(result.model);
                }

                var returnmodel = await _mediator.Send(new GetMerchandiseReturn()
                {
                    Order = order,
                    Language = _workContext.WorkingLanguage,
                    Store = _workContext.CurrentStore
                });
                returnmodel.Error = result.model.Error;
                returnmodel.Comments = model.Comments;
                returnmodel.PickupDate = model.PickupDate;
                returnmodel.NewAddressPreselected = model.NewAddressPreselected;
                returnmodel.NewAddress = model.NewAddress;
                if (returnmodel.NewAddressPreselected || _orderSettings.MerchandiseReturns_AllowToSpecifyPickupAddress)
                {
                    await PrepareModelAddress(model.NewAddress, address);
                }
                return View(returnmodel);
            }

        }

        public virtual async Task<IActionResult> MerchandiseReturnDetails(string merchandiseReturnId)
        {
            var rr = await _merchandiseReturnService.GetMerchandiseReturnById(merchandiseReturnId);
            if (!await rr.Access(_workContext.CurrentCustomer, _groupService))
                return Challenge();

            var order = await _orderService.GetOrderById(rr.OrderId);
            if (!await order.Access(_workContext.CurrentCustomer, _groupService))
                return Challenge();

            var model = await _mediator.Send(new GetMerchandiseReturnDetails()
            {
                Order = order,
                Language = _workContext.WorkingLanguage,
                MerchandiseReturn = rr,
            });

            return View(model);
        }

        #endregion
    }
}
