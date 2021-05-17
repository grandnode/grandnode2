using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Domain.Data
{
    public static class RepositoryExtensions
    {
        public static Task<List<T>> ToListAsync2<T>(this IQueryable<T> query)
        {
            return ((IMongoQueryable<T>)query).ToListAsync();
        }

        public static Task<List<T>> ToListAsync2<T>(this IMongoQueryable<T> query)
        {
            return query.ToListAsync();
        }

        public static Task<T> FirstOrDefaultAsync2<T>(this IMongoQueryable<T> query)
        {
            return query.FirstOrDefaultAsync();
        }

        public static Task<T> FirstOrDefaultAsync2<T>(this IOrderedQueryable<T> query)
        {
            return ((IMongoQueryable<T>)query).FirstOrDefaultAsync();
        }
    }
}
