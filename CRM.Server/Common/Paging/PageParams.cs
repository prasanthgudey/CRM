using System;

namespace CRM.Server.Common.Paging
{
    public class PageParams
    {
        public int Page { get; set; } = 1; // 1-based
        private int _pageSize = 20;
        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = Math.Clamp(value, 1, 200); // clamp globally
        }

        public string? Search { get; set; }
        public string? SortBy { get; set; }
        public string? SortDir { get; set; } = "desc"; // "asc" or "desc"
    }
}
