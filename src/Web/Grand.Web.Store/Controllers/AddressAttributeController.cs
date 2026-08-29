using Grand.Business.Core.Interfaces.Common.Addresses;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Common;
using Grand.Domain.Permissions;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Localization;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Store.Controllers;

[Area("Store")]
[PermissionAuthorize(PermissionSystemName.AddressAttributes)]
[AuthorizeStore]
public class AddressAttributeController(
    IAddressAttributeService addressAttributeService,
    IAddressAttributeViewModelService addressAttributeViewModelService,
    ILanguageService languageService,
    ITranslationService translationService,
    IAdminDataScope<AddressAttribute> scope)
    : BaseAddressAttributeController(addressAttributeService, addressAttributeViewModelService,
        languageService, translationService, scope);
