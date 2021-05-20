using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Domain
{
    public partial class PagedList<T> : List<T>, IPagedList<T>
    {
        private async Task InitializeAsync(IMongoQueryable<T> source, int pageIndex, int pageSize, int? totalCount = null)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (pageSize <= 0)
                pageSize = 1;

            TotalCount = totalCount ?? await source.CountAsync();
            source = totalCount == null ? source.Skip(pageIndex * pageSize).Take(pageSize) : source;
            AddRange(source);

            if (pageSize > 0)
            {
                TotalPages = TotalCount / pageSize;
                if (TotalCount % pageSize > 0)
                    TotalPages++;
            }

            PageSize = pageSize;
            PageIndex = pageIndex;
        }

        public static async Task<PagedList<T>> Create(IQueryable<T> source, int pageIndex, int pageSize, FindOptions findOptions = null)
        {
            var pagelist = new PagedList<T>();
            await pagelist.InitializeAsync((IMongoQueryable<T>)source, pageIndex, pageSize);
            return pagelist;
        }

        
    }
}
