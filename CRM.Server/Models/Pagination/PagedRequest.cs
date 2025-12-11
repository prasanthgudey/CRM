namespace CRM.Server.Models.Pagination
{
    // USER-DEFINED
    public class PagedRequest
    {
        // The page the client wants, starting from 1
        public int PageNumber { get; set; } = 1;

        // How many items per page
        public int PageSize { get; set; } = 10;
    }
}
