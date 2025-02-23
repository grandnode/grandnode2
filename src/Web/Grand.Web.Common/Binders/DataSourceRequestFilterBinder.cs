using Grand.Web.Common.DataSource;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using static Grand.Web.Common.DataSource.DataSourceRequestFilter;

namespace Grand.Web.Common.Binders;

public class DataSourceRequestFilterBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        if (bindingContext == null)
            throw new ArgumentNullException(nameof(bindingContext));

        var query = bindingContext.HttpContext.Request.Query;
        var model = new DataSourceRequestFilter();

        if (query.ContainsKey("filter[logic]"))
            model.Logic = query["filter[logic]"].FirstOrDefault();

        var filters = new List<FilterDescriptor>();
        var index = 0;
        while (true)
        {
            var fieldKey = $"filter[filters][{index}][field]";
            if (!query.ContainsKey(fieldKey))
                break;

            var operKey = $"filter[filters][{index}][operator]";
            var valueKey = $"filter[filters][{index}][value]";
            var ignoreCaseKey = $"filter[filters][{index}][ignoreCase]";

            var field = query[fieldKey].FirstOrDefault();
            var oper = query.ContainsKey(operKey) ? query[operKey].FirstOrDefault() : null;
            var value = query.ContainsKey(valueKey) ? query[valueKey].FirstOrDefault() : null;
            var ignoreCase = query.ContainsKey(ignoreCaseKey) && bool.TryParse(query[ignoreCaseKey].FirstOrDefault(), out var ic) && ic;

            filters.Add(new FilterDescriptor {
                Field = field,
                Operator = oper,
                Value = value,
                IgnoreCase = ignoreCase
            });
            index++;
        }
        model.Filters = filters;

        bindingContext.Result = ModelBindingResult.Success(model);
        return Task.CompletedTask;
    }
}