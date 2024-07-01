namespace Shelter.Models;

public class ResumeRequestModel
{
    public string AuthorId { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string AnimalType { get; set; }
    public string Price { get; set; }
    public string Category { get; set; }
    public string City { get; set; }
}