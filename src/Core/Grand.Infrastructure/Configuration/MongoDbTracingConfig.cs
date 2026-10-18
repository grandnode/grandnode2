using MongoDB.Driver;
using MongoDB.Driver.Core.Configuration;

namespace Grand.Infrastructure.Configuration;

/// <summary>
///     The "Telemetry:Instrumentation:MongoDb" section, applied where the MongoDB client is built. The telemetry pipeline
///     only listens to the driver when a sink is configured; whether the driver creates spans at all is decided here.
/// </summary>
public class MongoDbTracingConfig
{
    public const string SectionName = "Telemetry:Instrumentation:MongoDb";

    /// <summary>
    ///     Gets or sets a value indicating whether MongoDB operations produce spans. Off turns tracing off in the driver,
    ///     so no activities are created even for another listener in the process.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    ///     Gets or sets the maximum length of the query text (db.query.text) on MongoDB spans. 0 leaves it out - the
    ///     filters and documents of a query may hold customer data, so set it only where the backend may store them.
    /// </summary>
    public int QueryTextMaxLength { get; set; }

    public void ApplyTo(MongoClientSettings clientSettings)
    {
        ArgumentNullException.ThrowIfNull(clientSettings);

        if (QueryTextMaxLength < 0)
            throw new InvalidOperationException(
                $"{SectionName}:QueryTextMaxLength must be 0 or more, was {QueryTextMaxLength}.");

        if (!Enabled)
            clientSettings.TracingOptions = new TracingOptions { Disabled = true };
        else if (QueryTextMaxLength > 0)
            clientSettings.TracingOptions = new TracingOptions { QueryTextMaxLength = QueryTextMaxLength };
    }
}
