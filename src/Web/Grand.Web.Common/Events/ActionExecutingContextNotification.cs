using MediatR;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Grand.Web.Common.Events;

public class ActionExecutingContextNotification : INotification
{
    public ActionExecutingContextNotification(ActionExecutingContext context, bool before)
    {
        Context = context;
        Before = before;
    }

    public ActionExecutingContext Context { get; private set; }

    public bool Before { get; private set; }
}