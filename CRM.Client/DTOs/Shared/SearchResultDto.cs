namespace CRM.Client.DTOs.Shared
{
    public class SearchResultDto
    {
        public string EntityType { get; set; }
        public string Id { get; set; }
        public string Title { get; set; }
        public string Subtitle { get; set; }
        public string Snippet { get; set; }
        public string Route { get; set; }
        public DateTime? Date { get; set; }
    }
}
