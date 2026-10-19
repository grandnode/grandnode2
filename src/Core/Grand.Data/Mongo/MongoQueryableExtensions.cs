using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace Grand.Data.Mongo;

public static class MongoQueryableExtensions
{
    /// <summary>
    ///     Filters by the collection's text index ($text). The server accepts $text only in the first $match
    ///     of a pipeline, so call it before any other Where
    /// </summary>
    public static IQueryable<T> WhereText<T>(this IQueryable<T> query, string search)
    {
        var filter = Builders<T>.Filter.Text(search);
        return query.Where(_ => filter.Inject());
    }
}
