namespace Shelter.Models;

public class Advt
{
    public string id { get; set; }
    public string partitionKey { get; set; } = "advt";

    public string AuthorId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Price { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public String Date { get; set; } = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
    public string? Image { get; set; }
}