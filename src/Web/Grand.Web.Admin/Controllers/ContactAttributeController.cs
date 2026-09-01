using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Marketing.Contacts;
using Grand.Domain.Catalog;
using Grand.Domain.Messages;
using Grand.Domain.Permissions;
using Grand.Web.Admin.Extensions;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Admin.Controllers;

// Reduced to a thin subclass of BaseContactAttributeController (ARCH-001 ContactAttribute consolidation). All
// regions of behavior live in the shared base; this class only supplies Admin's DI wiring plus the
// attributes that used to arrive transitively via BaseAdminController - BaseContactAttributeController
// can't inherit any single host's base controller (it's shared across Admin/Store, each with a
// different [Area]/[Authorize*] pair), so each subclass restates its own host's attribute set
// explicitly. Same pattern as ProductController (see that file).
[AuthorizeAdmin]
[AutoValidateAntiforgeryToken]
[Area(Constants.AreaAdmin)]
[AuthorizeMenu]
public class ContactAttributeController(
    IContactAttributeViewModelService contactAttributeViewModelService,
    IContactAttributeService contactAttributeService,
    ILanguageService languageService,
    ITranslationService translationService,
    IAdminDataScope<ContactAttribute> scope)
    : BaseContactAttributeController(contactAttributeViewModelService, contactAttributeService,
        languageService, translationService, scope);