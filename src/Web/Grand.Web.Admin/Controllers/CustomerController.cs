using Grand.Web.Admin.Extensions;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.Common.Filters;
using Microsoft.AspNetCore.Mvc;
using Grand.Web.Common.Localization;
using Grand.Web.Common.Security.Authorization;

namespace Grand.Web.Admin.Controllers;

[AuthorizeAdmin]
[AutoValidateAntiforgeryToken]
[Area(Constants.AreaAdmin)]
[AuthorizeMenu]
public class CustomerController(
    Grand.Business.Core.Interfaces.Customers.ICustomerService customerService,
    Grand.Web.AdminShared.Interfaces.ICustomerViewModelService customerViewModelService,
    Grand.Business.Core.Interfaces.Customers.ICustomerManagerService customerManagerService,
    Grand.Business.Core.Interfaces.Customers.ICustomerAttributeParser customerAttributeParser,
    Grand.Business.Core.Interfaces.Customers.ICustomerAttributeService customerAttributeService,
    Grand.Business.Core.Interfaces.Messages.ICustomerMessageProviderService customerMessageProviderService,
    Grand.Business.Core.Interfaces.Common.Directory.IGroupService groupService,
    Grand.Web.Common.Localization.IRequestTranslationService translationService,
    Grand.Infrastructure.IContextAccessor contextAccessor,
    Grand.Domain.Customers.CustomerSettings customerSettings,
    Grand.Web.AdminShared.Interfaces.IAdminDataScope<Grand.Domain.Customers.Customer> scope,
    IRequestPermissionService permissionService,
    Grand.Business.Core.Interfaces.ExportImport.IExportManager<Grand.Domain.Customers.Customer> exportManager)
    : BaseCustomerManagementController(customerService, customerViewModelService, customerManagerService,
        customerAttributeParser, customerAttributeService, customerMessageProviderService, groupService,
        translationService, contextAccessor, customerSettings, scope, permissionService, exportManager);
