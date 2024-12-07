using Microsoft.AspNetCore.Http;

namespace Grand.Module.Installer.Interfaces;

public interface IInstallationService
{
    Task InstallData(
        string httpscheme, HostString host,
        string defaultUserEmail, string defaultUserPassword, string? collation, bool installSampleData = true,
        string? companyName = "", string? companyAddress = "", string? companyPhoneNumber = "", string? companyEmail = "");
}