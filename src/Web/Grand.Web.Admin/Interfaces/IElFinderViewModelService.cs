using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Admin.Interfaces
{
    public interface IElFinderViewModelService
    {
        Task SetupConnectorAsync();
        Task<IActionResult> Connector();
        Task<IActionResult> Thumbs(string id);
    }
}
