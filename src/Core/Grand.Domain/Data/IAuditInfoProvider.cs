namespace Grand.Domain.Data;

public interface IAuditInfoProvider
{
    string GetCurrentUser();
    DateTime GetCurrentDateTime();
}