namespace RaceDayAPI.Models;

public class Event
{
    public int EventID { get; set; }
    public int OrganiserID { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateOnly EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }

    public AppUser Organiser { get; set; } = null!;
    public ICollection<Category> Categories { get; set; } = new List<Category>();
}
