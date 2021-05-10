using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.WebUtilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Grand.Infrastructure.Extensions
{
    public static class CommonExtensions
    {
        public static string ModifyQueryString(string url, string key, string value)
        {
            if (string.IsNullOrEmpty(url))
                return string.Empty;

            if (string.IsNullOrEmpty(key))
                return url;

            var uri = new Uri(url);
            var baseUri = uri.GetComponents(UriComponents.Scheme | UriComponents.Host | UriComponents.Port | UriComponents.Path, UriFormat.UriEscaped);

            var query = QueryHelpers.ParseQuery(uri.Query);

            var items = query.SelectMany(x => x.Value, (col, val) =>
                new KeyValuePair<string, string>(col.Key, val)).ToList();

            items.RemoveAll(x => x.Key == key);

            var qb = new QueryBuilder(items);

            if (!string.IsNullOrEmpty(value))
                qb.Add(key, value);

            var returnUrl = baseUri + qb.ToQueryString();
            return returnUrl;
        }

        public static TResult Return<TInput, TResult>(this TInput o, Func<TInput, TResult> evaluator, TResult failureValue)
            where TInput : class
        {
            return o == null ? failureValue : evaluator(o);
        }

    }
}
