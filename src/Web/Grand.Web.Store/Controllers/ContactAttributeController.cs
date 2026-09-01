using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Marketing.Contacts;
using Grand.Domain.Catalog;
using Grand.Domain.Messages;
using Grand.Domain.Permissions;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Grand.Web.Store.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Store.Controllers;

// Reduced to a thin subclass of BaseContactAttributeController (ARCH-001 ContactAttribute consolidation). All
// regions of behavior live in the shared base; this class only supplies Store's DI wiring and the attributes
// that used to arrive transitively via BaseStoreController. Same pattern as ProductController (see that file).
[AutoValidateAntiforgeryToken]
[Area(Constants.AreaStore)]
[AuthorizeStore]
[AuthorizeMenu]
public class ContactAttributeController(
    IContactAttributeViewModelService contactAttributeViewModelService,
    IContactAttributeService contactAttributeService,
    ILanguageService languageService,
    ITranslationService translationService,
    IAdminDataScope<ContactAttribute> scope)
    : BaseContactAttributeController(contactAttributeViewModelService, contactAttributeService,
        languageService, translationService, scope);
