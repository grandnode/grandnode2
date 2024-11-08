namespace Grand.Infrastructure.Configuration;

public class FeatureFlagsConfig
{
    public Dictionary<string, bool> Modules { get; set; } = new Dictionary<string, bool>();
}
