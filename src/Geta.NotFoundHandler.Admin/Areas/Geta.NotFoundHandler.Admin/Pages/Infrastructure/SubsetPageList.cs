using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using X.PagedList;

namespace Geta.NotFoundHandler.Admin.Areas.Geta.NotFoundHandler.Admin.Pages.Infrastructure
{
    internal class SubsetPageList<T> : IPagedList<T>
    {
        private readonly IList<T> _items;

        public SubsetPageList(IEnumerable<T> subset, int pageNumber, int pageSize)
        {
            if (pageNumber < 1)
                throw new ArgumentException("cannot be less than 1", nameof(pageNumber));

            if (pageSize < 1)
                throw new ArgumentException("cannot be less than 1", nameof(pageSize));

            _items = subset.ToList();
            PageNumber = pageNumber;
            PageSize = pageSize;
        }

        public T this[int index] => _items[index];

        public int PageCount => HasNextPage ? PageNumber + 1 : PageNumber;

        public int TotalItemCount => (PageNumber - 1) * PageSize;

        public int PageNumber { get; }

        public int PageSize { get; }

        public bool HasPreviousPage => PageNumber > 1;

        public bool HasNextPage => _items.Count > PageSize;

        public bool IsFirstPage => PageNumber == 1;

        public bool IsLastPage => !HasNextPage;

        public int FirstItemOnPage => 0;

        public int LastItemOnPage => Math.Min(PageSize, _items.Count);

        public int Count => _items.Count;

        public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();
        
        public PagedListMetaData GetMetaData() => new(this);        
    }
}
