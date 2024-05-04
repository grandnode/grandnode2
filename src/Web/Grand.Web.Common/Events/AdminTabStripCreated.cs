using MediatR;
using Microsoft.AspNetCore.Html;

namespace Grand.Web.Common.Events;

/// <summary>
///     Admin tabstrip created event
/// </summary>
public class AdminTabStripCreated : INotification
{
    public AdminTabStripCreated(string tabStripName)
    {
        TabStripName = tabStripName;
        BlocksToRender = new List<(string tabname, IHtmlContent content)>();
    }

    public string TabStripName { get; private set; }
    public IList<(string tabname, IHtmlContent content)> BlocksToRender { get; set; }
}