namespace Grand.Infrastructure.Configuration;

public class ApplicationInsightsConfig
{
    public string ConnectionString { get; set; }
    public bool TrackDependencyMongoDb { get; set; }
}