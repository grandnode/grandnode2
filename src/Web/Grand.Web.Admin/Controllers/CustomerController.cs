using Grand.Web.Admin.Extensions;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.Common.Filters;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Admin.Controllers;

[AuthorizeAdmin]
[AutoValidateAntiforgeryToken]
[Area(Constants.AreaAdmin)]
[AuthorizeMenu]
public class CustomerController(
    Grand.Business.Core.Interfaces.Customers.ICustomerService customerService,
    Grand.Web.AdminShared.Interfaces.ICustomerViewModelService customerViewModelService,
    Grand.Business.Core.Interfaces.Customers.ICustomerManagerService customerManagerService,
    Grand.Business.Core.Interfaces.Marketing.Customers.ICustomerProductService customerProductService,
    Grand.Business.Core.Interfaces.Catalog.Products.IProductReviewService productReviewService,
    Grand.Web.AdminShared.Interfaces.IProductReviewViewModelService productReviewViewModelService,
    Grand.Web.AdminShared.Interfaces.IProductViewModelService productViewModelService,
    Grand.Business.Core.Interfaces.Customers.ICustomerAttributeParser customerAttributeParser,
    Grand.Business.Core.Interfaces.Customers.ICustomerAttributeService customerAttributeService,
    Grand.Business.Core.Interfaces.Common.Addresses.IAddressAttributeParser addressAttributeParser,
    Grand.Business.Core.Interfaces.Common.Addresses.IAddressAttributeService addressAttributeService,
    Grand.Business.Core.Interfaces.Messages.IMessageProviderService messageProviderService,
    Grand.Business.Core.Interfaces.Common.Directory.IGroupService groupService,
    Grand.Business.Core.Interfaces.Common.Localization.ITranslationService translationService,
    Grand.Infrastructure.IContextAccessor contextAccessor,
    Grand.Domain.Customers.CustomerSettings customerSettings,
    Grand.Web.AdminShared.Interfaces.IAdminDataScope<Grand.Domain.Customers.Customer> scope,
    Grand.Business.Core.Interfaces.Common.Security.IPermissionService permissionService,
    Grand.Business.Core.Interfaces.ExportImport.IExportManager<Grand.Domain.Customers.Customer> exportManager)
    : BaseCustomerManagementController(customerService, customerViewModelService, customerManagerService,
        customerProductService, productReviewService, productReviewViewModelService, productViewModelService,
        customerAttributeParser, customerAttributeService, addressAttributeParser, addressAttributeService,
        messageProviderService, groupService, translationService, contextAccessor, customerSettings, scope,
        permissionService, exportManager);
