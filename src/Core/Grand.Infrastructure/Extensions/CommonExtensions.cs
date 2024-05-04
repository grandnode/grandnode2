using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.WebUtilities;

namespace Grand.Infrastructure.Extensions;

public static class CommonExtensions
{
    public static string ModifyQueryString(string url, string key, string value)
    {
        if (string.IsNullOrEmpty(url))
            return string.Empty;

        if (string.IsNullOrEmpty(key))
            return url;

        var baseUrl = "";
        var queryString = "";

        if (Uri.IsWellFormedUriString(url, UriKind.Absolute))
        {
            var uri = new Uri(url);
            baseUrl = uri.AbsolutePath;
            queryString = uri.Query;
        }
        else
        {
            var questionMarkIndex = url.IndexOf('?');
            if (questionMarkIndex != -1)
            {
                baseUrl = url.Substring(0, questionMarkIndex);
                queryString = url.Substring(questionMarkIndex);
            }
            else
            {
                baseUrl = url;
            }
        }

        var query = QueryHelpers.ParseQuery(queryString);

        var items = query.SelectMany(x => x.Value, (col, val) => new KeyValuePair<string, string>(col.Key, val))
            .ToList();

        items.RemoveAll(x => x.Key == key);

        if (!string.IsNullOrEmpty(value)) items.Add(new KeyValuePair<string, string>(key, value));

        var qb = new QueryBuilder(items);

        return baseUrl + qb.ToQueryString();
    }
}