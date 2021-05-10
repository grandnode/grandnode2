using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Grand.Infrastructure;
using Grand.Domain.Catalog;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Web.Features.Models.Catalog;
using Grand.Web.Models.Catalog;
using MediatR;
using Microsoft.AspNetCore.Mvc.Rendering;
using Grand.Business.Common.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Grand.Infrastructure.Extensions;

namespace Grand.Web.Features.Handlers.Catalog
{
    public class GetViewSortSizeOptionsHandler : IRequestHandler<GetViewSortSizeOptions, (CatalogPagingFilteringModel pagingFilteringModel, CatalogPagingFilteringModel command)>
    {
        private readonly ITranslationService _translationService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly CatalogSettings _catalogSettings;

        public GetViewSortSizeOptionsHandler(ITranslationService translationService, CatalogSettings catalogSettings, IHttpContextAccessor httpContextAccessor)
        {
            _translationService = translationService;
            _catalogSettings = catalogSettings;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<(CatalogPagingFilteringModel pagingFilteringModel, CatalogPagingFilteringModel command)> Handle(GetViewSortSizeOptions request, CancellationToken cancellationToken)
        {
            PrepareSortingOptions(request);
            PrepareViewModes(request);
            PreparePageSizeOptions(request);

            return await Task.FromResult((request.PagingFilteringModel, request.Command));
        }

        private void PrepareSortingOptions(GetViewSortSizeOptions request)
        {
            if (request.PagingFilteringModel == null)
                throw new ArgumentNullException(nameof(request.PagingFilteringModel));

            if (request.Command == null)
                throw new ArgumentNullException(nameof(request.Command));

            var allDisabled = _catalogSettings.ProductSortingEnumDisabled.Count == Enum.GetValues(typeof(ProductSortingEnum)).Length;
            request.PagingFilteringModel.AllowProductSorting = _catalogSettings.AllowProductSorting && !allDisabled;

            var activeOptions = Enum.GetValues(typeof(ProductSortingEnum)).Cast<int>()
                .Except(_catalogSettings.ProductSortingEnumDisabled)
                .Select((idOption) =>
                {
                    int order;
                    return new KeyValuePair<int, int>(idOption, _catalogSettings.ProductSortingEnumDisplayOrder.TryGetValue(idOption, out order) ? order : idOption);
                })
                .OrderBy(x => x.Value);
            if (request.Command.OrderBy == null)
                request.Command.OrderBy = allDisabled ? 0 : activeOptions.First().Key;

            if (request.PagingFilteringModel.AllowProductSorting)
            {
                foreach (var option in activeOptions)
                {
                    var currentPageUrl = _httpContextAccessor.HttpContext.Request.GetDisplayUrl();
                    var sortUrl = CommonExtensions.ModifyQueryString(currentPageUrl, "orderby", (option.Key).ToString());

                    var sortValue = ((ProductSortingEnum)option.Key).GetTranslationEnum(_translationService, request.Language.Id);
                    request.PagingFilteringModel.AvailableSortOptions.Add(new SelectListItem {
                        Text = sortValue,
                        Value = sortUrl,
                        Selected = option.Key == request.Command.OrderBy
                    });
                }
            }
        }
        private void PrepareViewModes(GetViewSortSizeOptions request)
        {

            request.PagingFilteringModel.AllowProductViewModeChanging = _catalogSettings.AllowProductViewModeChanging;

            var viewMode = !string.IsNullOrEmpty(request.Command.ViewMode)
                ? request.Command.ViewMode
                : _catalogSettings.DefaultViewMode;
            request.PagingFilteringModel.ViewMode = viewMode;
            if (request.PagingFilteringModel.AllowProductViewModeChanging)
            {
                var currentPageUrl = _httpContextAccessor.HttpContext.Request.GetDisplayUrl();
                //grid
                request.PagingFilteringModel.AvailableViewModes.Add(new SelectListItem {
                    Text = _translationService.GetResource("Catalog.ViewMode.Grid"),
                    Value = CommonExtensions.ModifyQueryString(currentPageUrl, "viewmode", "grid"),
                    Selected = viewMode == "grid"
                });
                //list
                request.PagingFilteringModel.AvailableViewModes.Add(new SelectListItem {
                    Text = _translationService.GetResource("Catalog.ViewMode.List"),
                    Value = CommonExtensions.ModifyQueryString(currentPageUrl, "viewmode", "list"),
                    Selected = viewMode == "list"
                });
            }
        }
        private void PreparePageSizeOptions(GetViewSortSizeOptions request)
        {
            if (request.Command.PageNumber <= 0)
            {
                request.Command.PageNumber = 1;
            }
            request.PagingFilteringModel.AllowCustomersToSelectPageSize = false;
            if (request.AllowCustomersToSelectPageSize && request.PageSizeOptions != null)
            {
                var pageSizes = request.PageSizeOptions.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);

                if (pageSizes.Any())
                {
                    // get the first page size entry to use as the default (category page load) or if customer enters invalid value via query string
                    if (request.Command.PageSize <= 0 || !pageSizes.Contains(request.Command.PageSize.ToString()))
                    {
                        int temp;
                        if (int.TryParse(pageSizes.FirstOrDefault(), out temp))
                        {
                            if (temp > 0)
                            {
                                request.Command.PageSize = temp;
                            }
                        }
                    }

                    var currentPageUrl = _httpContextAccessor.HttpContext.Request.GetDisplayUrl();
                    var pageUrl = CommonExtensions.ModifyQueryString(currentPageUrl, "pagenumber", null);
                    foreach (var pageSize in pageSizes)
                    {
                        int temp;
                        if (!int.TryParse(pageSize, out temp))
                        {
                            continue;
                        }
                        if (temp <= 0)
                        {
                            continue;
                        }

                        request.PagingFilteringModel.PageSizeOptions.Add(new SelectListItem {
                            Text = pageSize,
                            Value = CommonExtensions.ModifyQueryString(pageUrl, "pagesize", pageSize),
                            Selected = pageSize.Equals(request.Command.PageSize.ToString(), StringComparison.OrdinalIgnoreCase)
                        });
                    }

                    if (request.PagingFilteringModel.PageSizeOptions.Any())
                    {
                        request.PagingFilteringModel.PageSizeOptions = request.PagingFilteringModel.PageSizeOptions.OrderBy(x => int.Parse(x.Text)).ToList();
                        request.PagingFilteringModel.AllowCustomersToSelectPageSize = true;

                        if (request.Command.PageSize <= 0)
                        {
                            request.Command.PageSize = int.Parse(request.PagingFilteringModel.PageSizeOptions.FirstOrDefault().Text);
                        }
                    }
                }
            }
            else
            {
                //customer is not allowed to select a page size
                request.Command.PageSize = request.PageSize;
            }

            //ensure pge size is specified
            if (request.Command.PageSize <= 0)
            {
                request.Command.PageSize = request.PageSize;
            }
        }

    }
}
