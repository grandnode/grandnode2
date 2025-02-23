using Grand.Web.Common.Binders;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Common.DataSource;

[ModelBinder(BinderType = typeof(DataSourceRequestFilterBinder))]
public class DataSourceRequestFilter
{
    public string Logic { get; set; }
    public List<FilterDescriptor> Filters { get; set; }

    public class FilterDescriptor
    {
        public string Field { get; set; }
        public string Operator { get; set; }
        public string Value { get; set; }
        public bool IgnoreCase { get; set; }
    }
}

public static class DataSourceRequestFilterExtensions
{
    public static string GetNameFilterValue(this DataSourceRequestFilter filter)
    {
        return filter.Filters.FirstOrDefault(x => x.Field == "Name")?.Value;
    }
}