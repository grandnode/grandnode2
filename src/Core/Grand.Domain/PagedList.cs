using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Domain
{
    /// <summary>
    /// Paged list
    /// </summary>
    /// <typeparam name="T">T</typeparam>
    [Serializable]
    public partial class PagedList<T> : List<T>, IPagedList<T>
    {

        private void Initialize(IEnumerable<T> source, int pageIndex, int pageSize, int? totalCount = null)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            if (pageSize <= 0)
                pageSize = 1;

            TotalCount = totalCount ?? source.Count();

            if (pageSize > 0)
            {
                TotalPages = TotalCount / pageSize;
                if (TotalCount % pageSize > 0)
                    TotalPages++;
            }

            PageSize = pageSize;
            PageIndex = pageIndex;
            source = totalCount == null ? source.Skip(pageIndex * pageSize).Take(pageSize) : source;
            AddRange(source);
        }

        public PagedList()
        {
        }
        public PagedList(IEnumerable<T> source, int pageIndex, int pageSize)
        {
            Initialize(source, pageIndex, pageSize);
        }

        public PagedList(IEnumerable<T> source, int pageIndex, int pageSize, int totalCount)
        {
            Initialize(source, pageIndex, pageSize, totalCount);
        }

        private Task InitializeAsync(IQueryable<T> source, int pageIndex, int pageSize, int? totalCount = null)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (pageSize <= 0)
                pageSize = 1;

            TotalCount = totalCount ?? source.Count();
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
            return Task.CompletedTask;
        }

        public static async Task<PagedList<T>> Create(IQueryable<T> source, int pageIndex, int pageSize)
        {
            var pagelist = new PagedList<T>();
            await pagelist.InitializeAsync(source, pageIndex, pageSize);
            return pagelist;
        }

        public int PageIndex { get; protected set; }
        public int PageSize { get; protected set; }
        public int TotalCount { get; protected set; }
        public int TotalPages { get; protected set; }

        public bool HasPreviousPage {
            get { return (PageIndex > 0); }
        }
        public bool HasNextPage {
            get { return (PageIndex + 1 < TotalPages); }
        }
    }
}
