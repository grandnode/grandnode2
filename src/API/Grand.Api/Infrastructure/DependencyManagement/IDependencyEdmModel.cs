using Grand.Infrastructure.Configuration;
using Microsoft.AspNet.OData.Builder;

namespace Grand.Api.Infrastructure.DependencyManagement
{
    public interface IDependencyEdmModel
    {
        /// <summary>
        /// Register edmmodel
        /// </summary>
        /// <param name="builder">OData Convention Model Builder</param>
        void Register(ODataConventionModelBuilder builder, ApiConfig apiConfig);

        /// <summary>
        /// Order of this dependency implementation
        /// </summary>
        int Order { get; }
    }
}
