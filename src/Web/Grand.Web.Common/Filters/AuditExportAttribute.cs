using Grand.Infrastructure.Audit;
using Grand.Web.Common.Audit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Collections;

namespace Grand.Web.Common.Filters;

/// <summary>
///     Records an audit Export event when the action hands out a file, with a summary of the filter it was given
/// </summary>
public class AuditExportAttribute : TypeFilterAttribute
{
    public AuditExportAttribute(string entityType) : base(typeof(AuditExportFilter))
    {
        Arguments = [entityType];
    }

    public class AuditExportFilter(string entityType, IAuditEventRecorder recorder) : IAsyncActionFilter
    {
        private const int MaxSummaryLength = 500;

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            //the summary is only for the log, so a model it cannot read must not fail the export
            string summary;
            try
            {
                summary = Summarize(context.ActionArguments);
            }
            catch (Exception)
            {
                summary = "unreadable";
            }

            var executed = await next();
            if (executed.Exception == null && executed.Result is FileResult)
                await recorder.Export(entityType, summary, null);
        }

        private static string Summarize(IDictionary<string, object> arguments)
        {
            var parts = new List<string>();
            foreach (var (name, value) in arguments)
            {
                if (value == null || value is IFormFile or CancellationToken)
                    continue;
                if (IsSimple(value.GetType()))
                    Add(parts, name, value);
                else if (value is not IEnumerable)
                    foreach (var property in value.GetType().GetProperties()
                                 .Where(p => p.CanRead && p.GetIndexParameters().Length == 0))
                        Add(parts, property.Name, property.GetValue(value));
            }

            var summary = string.Join("; ", parts);
            return summary.Length > MaxSummaryLength ? summary[..MaxSummaryLength] : summary;
        }

        private static void Add(List<string> parts, string name, object value)
        {
            if (value == null || AuditDiff.IsSensitive(name))
                return;

            var type = value.GetType();
            if (IsSimple(type))
            {
                if (value is string text && string.IsNullOrEmpty(text))
                    return;
                if (type.IsValueType && !type.IsEnum && Equals(value, Activator.CreateInstance(type)))
                    return;
                parts.Add($"{name}={value}");
            }
            else if (value is ICollection { Count: > 0 } collection)
            {
                parts.Add($"{name}: {collection.Count} items");
            }
        }

        private static bool IsSimple(Type type)
        {
            type = Nullable.GetUnderlyingType(type) ?? type;
            return type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal)
                   || type == typeof(DateTime) || type == typeof(Guid);
        }
    }
}
