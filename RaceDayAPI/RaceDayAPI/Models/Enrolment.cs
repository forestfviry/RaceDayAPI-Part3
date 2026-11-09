namespace RaceDayAPI.Models;

public class Enrolment
{
    public int EnrolmentID { get; set; }
    public int UserID { get; set; }
    public int CategoryID { get; set; }
    public DateTime EnrolmentDate { get; set; }
    public string Status { get; set; } = EnrolmentStatuses.Registered;

    public AppUser User { get; set; } = null!;
    public Category Category { get; set; } = null!;
    public Result? Result { get; set; }
}
