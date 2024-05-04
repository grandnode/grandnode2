using Grand.Infrastructure.Configuration;
using Microsoft.OData.ModelBuilder;

namespace Grand.Api.Infrastructure.DependencyManagement;

public interface IDependencyEdmModel
{
    /// <summary>
    ///     Order of this dependency implementation
    /// </summary>
    int Order { get; }

    /// <summary>
    ///     Register edmmodel
    /// </summary>
    /// <param name="builder">OData Convention Model Builder</param>
    /// <param name="apiConfig">ApiConfig</param>
    void Register(ODataConventionModelBuilder builder, BackendAPIConfig apiConfig);
}