using RaceDayAPI.DTOs;
using RaceDayAPI.Models;

namespace RaceDayAPI.Extensions;

public static class Mappings
{
    public static UserDto ToDto(this AppUser u) =>
        new(u.UserID, u.FullName, u.Email, u.PhoneNumber, u.Role.RoleName, u.CreatedAt);

    public static EventDto ToDto(this Event e) =>
        new(e.EventID, e.OrganiserID, e.EventName, e.EventDate, e.Location, e.Description, e.CreatedAt);

    public static CategoryDto ToDto(this Category c) =>
        new(c.CategoryID, c.EventID, c.CategoryName, c.DistanceKm, c.MaxParticipants, c.EntryFee);

    /// <summary>Requires User, Category and Category.Event to be loaded.</summary>
    public static EnrolmentDto ToDto(this Enrolment e) =>
        new(e.EnrolmentID, e.UserID, e.User.FullName, e.CategoryID, e.Category.CategoryName,
            e.Category.EventID, e.Category.Event.EventName, e.EnrolmentDate, e.Status);

    /// <summary>Requires Enrolment and Enrolment.User to be loaded.</summary>
    public static ResultDto ToDto(this Result r) =>
        new(r.ResultID, r.EnrolmentID, r.Enrolment.User.FullName, r.FinishTime, r.Position, r.Status);
}
