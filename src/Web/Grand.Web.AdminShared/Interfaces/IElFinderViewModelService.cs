using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.AdminShared.Interfaces;

public interface IElFinderViewModelService
{
    Task SetupConnectorAsync();
    Task<IActionResult> Connector();
    Task<IActionResult> Thumbs(string id);
}