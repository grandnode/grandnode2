namespace Grand.Infrastructure.Configuration;

public class BackendAPIConfig
{
    public bool Enabled { get; set; }
    public string SecretKey { get; set; }
    public bool ValidateIssuer { get; set; }
    public string ValidIssuer { get; set; }
    public bool ValidateAudience { get; set; }
    public string ValidAudience { get; set; }
    public int ExpiryInMinutes { get; set; }
}