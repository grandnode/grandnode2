using Grand.Business.Catalog.Interfaces.Tax;
using Grand.Business.Catalog.Utilities;
using Grand.Infrastructure.Plugins;
using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grand.Business.Catalog.Tests.Service.Tax
{
    public class FixedRateTestTaxProvider : BasePlugin, ITaxProvider
    {
        string IProvider.ConfigurationUrl => "";

        public string SystemName => "test-provider";

        public string FriendlyName => "tax provider";

        public int Priority => 100;

        public IList<string> LimitedToStores => new List<string>();

        public IList<string> LimitedToGroups => new List<string>();

      
        /// <summary>
        /// Gets a tax rate
        /// </summary>
        /// <param name="taxCategoryId">The tax category identifier</param>
        /// <returns>Tax rate</returns>
        protected double GetTaxRate(string taxCategoryId)
        {
            if (string.IsNullOrEmpty(taxCategoryId))
                return 0;

            double rate = 10;
            return rate;
        }

        /// <summary>
        /// Gets a route for provider configuration
        /// </summary>
        /// <param name="actionName">Action name</param>
        /// <param name="controllerName">Controller name</param>
        /// <param name="routeValues">Route values</param>
        public void GetConfigurationRoute(out string actionName, out string controllerName, out RouteValueDictionary routeValues)
        {
            actionName = null;
            controllerName = null;
            routeValues = null;
        }

        public Task<TaxResult> GetTaxRate(TaxRequest calculateTaxRequest)
        {
            var result = new TaxResult
            {
                TaxRate = GetTaxRate(calculateTaxRequest.TaxCategoryId)
            };
            return Task.FromResult(result);
        }
    }
}
