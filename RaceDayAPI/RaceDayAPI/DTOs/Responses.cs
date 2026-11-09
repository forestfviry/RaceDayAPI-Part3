namespace RaceDayAPI.DTOs;

public record UserDto(int UserId, string FullName, string Email, string? PhoneNumber, string Role, DateTime CreatedAt);

public record AuthResponse(string Token, DateTime ExpiresAt, UserDto User);

public record EventDto(int EventId, int OrganiserId, string EventName, DateOnly EventDate, string Location, string? Description, DateTime CreatedAt);

public record CategoryDto(int CategoryId, int EventId, string CategoryName, decimal DistanceKm, int MaxParticipants, decimal EntryFee);

public record EnrolmentDto(int EnrolmentId, int UserId, string ParticipantName, int CategoryId, string CategoryName, int EventId, string EventName, DateTime EnrolmentDate, string Status);

public record ResultDto(int ResultId, int EnrolmentId, string ParticipantName, TimeSpan? FinishTime, int? Position, string Status);
