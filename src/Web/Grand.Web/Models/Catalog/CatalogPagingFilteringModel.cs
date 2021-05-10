using Grand.Business.Catalog.Interfaces.Products;
using Grand.Business.Common.Extensions;
using Grand.Web.Common.Page.Paging;
using Grand.Domain.Catalog;
using Grand.Infrastructure;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Extensions;
using Grand.Infrastructure.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Web.Models.Catalog
{
    public partial class CatalogPagingFilteringModel : BasePageableModel
    {
        #region Constructors

        public CatalogPagingFilteringModel()
        {
            AvailableSortOptions = new List<SelectListItem>();
            AvailableViewModes = new List<SelectListItem>();
            PageSizeOptions = new List<SelectListItem>();
            SpecificationFilter = new SpecificationFilterModel();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Specification filter model
        /// </summary>
        public SpecificationFilterModel SpecificationFilter { get; set; }

        public bool AllowProductSorting { get; set; }
        public IList<SelectListItem> AvailableSortOptions { get; set; }
        public string AvailableSortOption { get; set; }

        public bool AllowProductViewModeChanging { get; set; }
        public IList<SelectListItem> AvailableViewModes { get; set; }

        public bool AllowCustomersToSelectPageSize { get; set; }
        public IList<SelectListItem> PageSizeOptions { get; set; }

        public string PageSizeOption { get; set; }

        /// <summary>
        /// Order by
        /// </summary>
        public int? OrderBy { get; set; }

        /// <summary>
        /// Product sorting
        /// </summary>
        public string ViewMode { get; set; }


        #endregion

        #region Nested classes

        public partial class SpecificationFilterModel : BaseModel
        {
            #region Ctor

            public SpecificationFilterModel()
            {
                AlreadyFilteredItems = new List<SpecificationFilterItem>();
                NotFilteredItems = new List<SpecificationFilterItem>();
            }

            #endregion

            #region Utilities

            protected virtual string ExcludeQueryStringParams(string url)
            {
                //comma separated list of parameters to exclude
                const string excludedQueryStringParams = "pagenumber";
                var excludedQueryStringParamsSplitted = excludedQueryStringParams.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string exclude in excludedQueryStringParamsSplitted)
                    url = CommonExtensions.ModifyQueryString(url, exclude, null);
                return url;
            }

            protected virtual string GenerateFilteredSpecQueryParam(IList<string> optionIds)
            {
                if (optionIds == null)
                    return "";

                string result = string.Join(",", optionIds);
                return result;
            }

            #endregion

            #region Methods

            public virtual async Task<List<string>> GetAlreadyFilteredSpecOptionIds
                (IHttpContextAccessor httpContextAccessor, ISpecificationAttributeService specificationAttributeService)
            {
                var result = new List<string>();

                foreach (var item in httpContextAccessor.HttpContext.Request.Query)
                {
                    var spec = await specificationAttributeService.GetSpecificationAttributeBySeName(item.Key);
                    if (spec != null)
                    {
                        foreach (var value in item.Value)
                        {
                            foreach (var option in value.Split(","))
                            {
                                var opt = spec.SpecificationAttributeOptions.FirstOrDefault(x => x.SeName == option.ToLowerInvariant());
                                if (opt != null)
                                {
                                    if (!result.Contains(opt.Id))
                                        result.Add(opt.Id);
                                }

                            }
                        }
                    }
                }

                return result;
            }

            public virtual async Task PrepareSpecsFilters(IList<string> alreadyFilteredSpecOptionIds,
                IList<string> filterableSpecificationAttributeOptionIds,
                ISpecificationAttributeService specificationAttributeService,
                IHttpContextAccessor httpContextAccessor,
                ICacheBase cacheBase, string langId)
            {
                Enabled = false;

                var allFilters = new List<SpecificationAttributeOptionFilter>();
                foreach (var sao in filterableSpecificationAttributeOptionIds.Union(alreadyFilteredSpecOptionIds))
                {
                    var sa = await specificationAttributeService.GetSpecificationAttributeByOptionId(sao);
                    if (sa != null)
                    {
                        allFilters.Add(new SpecificationAttributeOptionFilter
                        {
                            SpecificationAttributeId = sa.Id,
                            SpecificationAttributeName = sa.GetTranslation(x => x.Name, langId),
                            SpecificationAttributeSeName = sa.SeName,
                            SpecificationAttributeDisplayOrder = sa.DisplayOrder,
                            SpecificationAttributeOptionId = sao,
                            SpecificationAttributeOptionName = sa.SpecificationAttributeOptions.FirstOrDefault(x => x.Id == sao).GetTranslation(x => x.Name, langId),
                            SpecificationAttributeOptionSeName = sa.SpecificationAttributeOptions.FirstOrDefault(x => x.Id == sao).SeName,
                            SpecificationAttributeOptionDisplayOrder = sa.SpecificationAttributeOptions.FirstOrDefault(x => x.Id == sao).DisplayOrder,
                            SpecificationAttributeOptionColorRgb = sa.SpecificationAttributeOptions.FirstOrDefault(x => x.Id == sao).ColorSquaresRgb,
                        });
                    }
                }

                if (!allFilters.Any())
                    return;

                //sort loaded options
                allFilters = allFilters.OrderBy(saof => saof.SpecificationAttributeDisplayOrder)
                    .ThenBy(saof => saof.SpecificationAttributeName)
                    .ThenBy(saof => saof.SpecificationAttributeOptionDisplayOrder)
                    .ThenBy(saof => saof.SpecificationAttributeOptionName).ToList();

                //prepare the model properties
                Enabled = true;
                string removeFilterUrl = httpContextAccessor.HttpContext.Request.GetDisplayUrl();
                foreach (var item in allFilters.GroupBy(x => x.SpecificationAttributeSeName))
                {
                    removeFilterUrl = CommonExtensions.ModifyQueryString(removeFilterUrl, item.Key, null);
                }
                RemoveFilterUrl = ExcludeQueryStringParams(removeFilterUrl);

                //get already filtered specification options
                var alreadyFilteredOptions = allFilters.Where(x => alreadyFilteredSpecOptionIds.Contains(x.SpecificationAttributeOptionId));
                AlreadyFilteredItems = alreadyFilteredOptions.Select(x =>
                {
                    var alreadyFiltered = alreadyFilteredOptions.Where(y => y.SpecificationAttributeId == x.SpecificationAttributeId).Select(z => z.SpecificationAttributeOptionSeName)
                    .Except(new List<string> { x.SpecificationAttributeOptionSeName }).ToList();

                    var filterUrl = CommonExtensions.ModifyQueryString(httpContextAccessor.HttpContext.Request.GetDisplayUrl(), x.SpecificationAttributeSeName, GenerateFilteredSpecQueryParam(alreadyFiltered));

                    return new SpecificationFilterItem
                    {
                        SpecificationAttributeName = x.SpecificationAttributeName,
                        SpecificationAttributeSeName = x.SpecificationAttributeSeName,
                        SpecificationAttributeOptionName = x.SpecificationAttributeOptionName,
                        SpecificationAttributeOptionSeName = x.SpecificationAttributeOptionSeName,
                        SpecificationAttributeOptionColorRgb = x.SpecificationAttributeOptionColorRgb,
                        FilterUrl = ExcludeQueryStringParams(filterUrl)
                    };
                }).ToList();

                //get not filtered specification options
                NotFilteredItems = allFilters.Except(alreadyFilteredOptions).Select(x =>
                {
                    //filter URL
                    var alreadyFiltered = alreadyFilteredOptions.Where(y => y.SpecificationAttributeId == x.SpecificationAttributeId).Select(x => x.SpecificationAttributeOptionSeName)
                    .Concat(new List<string> { x.SpecificationAttributeOptionSeName });

                    var filterUrl = CommonExtensions.ModifyQueryString(httpContextAccessor.HttpContext.Request.GetDisplayUrl(), x.SpecificationAttributeSeName, GenerateFilteredSpecQueryParam(alreadyFiltered.ToList()));
                    return new SpecificationFilterItem()
                    {
                        SpecificationAttributeName = x.SpecificationAttributeName,
                        SpecificationAttributeSeName = x.SpecificationAttributeSeName,
                        SpecificationAttributeOptionName = x.SpecificationAttributeOptionName,
                        SpecificationAttributeOptionSeName = x.SpecificationAttributeOptionSeName,
                        SpecificationAttributeOptionColorRgb = x.SpecificationAttributeOptionColorRgb,
                        FilterUrl = ExcludeQueryStringParams(filterUrl)
                    };
                }).ToList();
            }

            #endregion

            #region Properties
            public bool Enabled { get; set; }
            public IList<SpecificationFilterItem> AlreadyFilteredItems { get; set; }
            public IList<SpecificationFilterItem> NotFilteredItems { get; set; }
            public string RemoveFilterUrl { get; set; }

            #endregion
        }

        public partial class SpecificationFilterItem : BaseModel
        {
            public string SpecificationAttributeName { get; set; }
            public string SpecificationAttributeSeName { get; set; }
            public string SpecificationAttributeOptionName { get; set; }
            public string SpecificationAttributeOptionSeName { get; set; }
            public string SpecificationAttributeOptionColorRgb { get; set; }
            public string FilterUrl { get; set; }
        }

        #endregion
    }
}