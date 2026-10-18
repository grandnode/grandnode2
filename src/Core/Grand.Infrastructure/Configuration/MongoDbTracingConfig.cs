using MongoDB.Driver;
using MongoDB.Driver.Core.Configuration;

namespace Grand.Infrastructure.Configuration;

/// <summary>
///     The part of "Telemetry:Instrumentation:MongoDb" applied where the MongoDB client is built. Whether the spans are
///     exported at all is decided by the telemetry pipeline (Telemetry:Instrumentation:MongoDb:Enabled).
/// </summary>
public class MongoDbTracingConfig
{
    public const string SectionName = "Telemetry:Instrumentation:MongoDb";

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

        if (QueryTextMaxLength > 0)
            clientSettings.TracingOptions = new TracingOptions { QueryTextMaxLength = QueryTextMaxLength };
    }
}
