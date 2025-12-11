using System;
using System.Collections.Generic;

namespace CRM.Server.Models.Pagination
{
    // USER-DEFINED: response wrapper for paged endpoints
    public class PagedResponse<T>
    {
        // Minimal payload — keep this small to reduce network cost
        public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();

        // Total number of items across all pages (after filters applied)
        public int TotalCount { get; set; }

        // Echoed request values
        public int PageNumber { get; set; }
        public int PageSize { get; set; }

        // Computed property (safe against division by zero)
        public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize);
    }
}
