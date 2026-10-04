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