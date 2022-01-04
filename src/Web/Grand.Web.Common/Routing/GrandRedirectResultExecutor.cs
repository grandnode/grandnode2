using Grand.Infrastructure.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Grand.Web.Common.Routing
{
    /// <summary>
    /// Represents custom overridden redirect result executor
    /// </summary>
    public class GrandRedirectResultExecutor : RedirectResultExecutor
    {
        #region Fields

        private readonly AppConfig _config;

        #endregion

        #region Ctor

        public GrandRedirectResultExecutor(ILoggerFactory loggerFactory,
            IUrlHelperFactory urlHelperFactory,
            AppConfig config) : base(loggerFactory, urlHelperFactory)
        {
            _config = config;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Execute passed redirect result
        /// </summary>
        /// <param name="context">Action context</param>
        /// <param name="result">Redirect result</param>
        public override Task ExecuteAsync(ActionContext context, RedirectResult result)
        {
            if (result == null)
                throw new ArgumentNullException(nameof(result));

            if (_config.AllowNonAsciiCharInHeaders)
            {
                result.Url = Flurl.Url.EncodeIllegalCharacters(result.Url);
            }
            return base.ExecuteAsync(context, result);
        }

        #endregion
    }
}
