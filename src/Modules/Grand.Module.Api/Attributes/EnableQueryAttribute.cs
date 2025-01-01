using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Dynamic.Core;
using Microsoft.AspNetCore.Http;

namespace Grand.Module.Api.Attributes;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class EnableQueryAttribute : ActionFilterAttribute
{
    public override void OnActionExecuted(ActionExecutedContext context)
    {
        if (context.Result is not ObjectResult result || result.Value == null)
            return;

        if (result.Value is IQueryable queryable)
        {
            queryable = ApplyQueryOptions(queryable, context.HttpContext.Request.Query, context.HttpContext.Response);
            result.Value = queryable;
        }
    }

    private IQueryable ApplyQueryOptions(IQueryable queryable, IQueryCollection query, HttpResponse response)
    {
        if (query.TryGetValue("$filter", out var filter))
            queryable = queryable.Where(filter.ToString());

        if (query.TryGetValue("$orderby", out var orderBy))
            queryable = queryable.OrderBy(orderBy.ToString());

        if (query.TryGetValue("$select", out var select))
            queryable = queryable.Select($"new({select})");

        if (query.TryGetValue("$skip", out var skipValue) && int.TryParse(skipValue, out var skip))
            queryable = queryable.Skip(skip);

        if (query.TryGetValue("$top", out var topValue) && int.TryParse(topValue, out var top))
            queryable = queryable.Take(top);

        if (query.TryGetValue("$count", out var countValue) && bool.TryParse(countValue, out var includeCount) && includeCount)
        {
            var totalCount = queryable.Count();
            response?.Headers?.Append("X-Total-Count", totalCount.ToString());
        }

        return queryable;
    }
}
