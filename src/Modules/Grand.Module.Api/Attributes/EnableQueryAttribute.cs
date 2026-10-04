using Grand.Module.Api.Constants;
using Grand.Module.Api.Queries;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Linq.Dynamic.Core;
using System.Linq.Dynamic.Core.Exceptions;

namespace Grand.Module.Api.Attributes;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class EnableQueryAttribute : ActionFilterAttribute
{
    public override void OnActionExecuted(ActionExecutedContext context)
    {
        if (context.Result is not ObjectResult result || result.Value == null)
            return;

        if (result.Value is not IQueryable queryable)
            return;

        try
        {
            result.Value = ApplyQueryOptions(queryable, context.HttpContext.Request.Query);
        }
        catch (ApiQueryOptionException ex)
        {
            //a rejected query option is the client's mistake, not ours - this filter runs after the
            //action, so without this the exception would leave the pipeline as a 500
            context.Result = new BadRequestObjectResult(new { error = ex.Message });
        }
        catch (ParseException ex)
        {
            context.Result = new BadRequestObjectResult(new { error = $"$filter could not be parsed: {ex.Message}" });
        }
    }

    private static IQueryable ApplyQueryOptions(IQueryable queryable, IQueryCollection query)
    {
        var elementType = queryable.ElementType;

        if (query.TryGetValue("$filter", out var filter))
        {
            ApiQueryOptions.ValidateFilter(filter.ToString(), elementType);
            queryable = queryable.Where(ApiQueryOptions.FilterConfig, filter.ToString());
        }

        if (query.TryGetValue("$orderby", out var orderBy))
            queryable = queryable.OrderBy(ApiQueryOptions.FilterConfig,
                ApiQueryOptions.ParseOrderBy(orderBy.ToString(), elementType));

        if (query.TryGetValue("$select", out var select))
            queryable = queryable.Select(ApiQueryOptions.SelectConfig,
                ApiQueryOptions.ParseSelect(select.ToString(), elementType));

        if (query.TryGetValue("$skip", out var skipValue))
        {
            if (!int.TryParse(skipValue, out var skip) || skip < 0)
                throw new ApiQueryOptionException("$skip must be a non-negative integer");

            queryable = queryable.Skip(skip);
        }

        if (query.TryGetValue("$top", out var topValue))
        {
            if (!int.TryParse(topValue, out var top) || top < 0)
                throw new ApiQueryOptionException("$top must be a non-negative integer");

            queryable = queryable.Take(Math.Min(top, Configurations.MaxLimit));
        }
        else
        {
            queryable = queryable.Take(Configurations.MaxLimit);
        }

        return queryable;
    }
}
