namespace Grand.Data.Tests;

public class AuditInfoProvider : IAuditInfoProvider
{
    public AuditInfoProvider()
    {
    }

    public string GetCurrentUser()
    {
        return "user";
    }

    public DateTime GetCurrentDateTime()
    {
        return DateTime.UtcNow;
    }
}