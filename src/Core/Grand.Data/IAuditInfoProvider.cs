namespace Grand.Data;

public interface IAuditInfoProvider
{
    string GetCurrentUser();
    DateTime GetCurrentDateTime();
}