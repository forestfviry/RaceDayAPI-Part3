namespace RaceDayAPI.Models;

public static class RoleNames
{
    public const string Organiser = "Organiser";
    public const string Participant = "Participant";
}

public static class EnrolmentStatuses
{
    public const string Registered = "Registered";
}

public static class ResultStatuses
{
    public const string Pending = "Pending";
    public const string Finished = "Finished";

    public static readonly string[] All = { "Pending", "Finished", "DNF", "DNS", "Disqualified" };
}
