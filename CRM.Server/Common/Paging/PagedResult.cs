using System;
using System.Collections.Generic;

namespace CRM.Server.Common.Paging
{
    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int Page { get; set; }            // 1-based
        public int PageSize { get; set; }
        public long TotalCount { get; set; }
        public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize);
    }
}
