namespace Shelter.Models;

public class SavedAvts
{
    public string id { get; set; }
    public string partitionKey { get; set; } = "savedadvt";
    public string advtId { get; set; }
    public string userId { get; set; }
}