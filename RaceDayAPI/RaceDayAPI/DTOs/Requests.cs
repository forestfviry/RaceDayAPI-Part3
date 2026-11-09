using System.ComponentModel.DataAnnotations;

namespace RaceDayAPI.DTOs;

public class RegisterRequest
{
    [Required, StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(100)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters.")]
    public string Password { get; set; } = string.Empty;

    /// <summary>Either "Organiser" or "Participant".</summary>
    [Required]
    public string Role { get; set; } = string.Empty;

    [Phone, StringLength(20)]
    public string? PhoneNumber { get; set; }
}

public class LoginRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class UpdateProfileRequest
{
    [Required, StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Phone, StringLength(20)]
    public string? PhoneNumber { get; set; }
}

public class EventRequest
{
    [Required, StringLength(100)]
    public string EventName { get; set; } = string.Empty;

    /// <summary>Format: yyyy-MM-dd</summary>
    [Required]
    public DateOnly? EventDate { get; set; }

    [Required, StringLength(150)]
    public string Location { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }
}

public class CategoryRequest
{
    [Required, StringLength(50)]
    public string CategoryName { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.01", "999.99", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    public decimal DistanceKm { get; set; }

    [Range(1, 100000)]
    public int MaxParticipants { get; set; } = 100;

    [Range(typeof(decimal), "0", "999999.99", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    public decimal EntryFee { get; set; }
}

public class ResultRequest
{
    /// <summary>Format: hh:mm:ss, e.g. 00:24:35</summary>
    public TimeSpan? FinishTime { get; set; }

    [Range(1, int.MaxValue)]
    public int? Position { get; set; }

    /// <summary>Pending, Finished, DNF, DNS or Disqualified. Defaults to Finished when a finish time is given, otherwise Pending.</summary>
    [StringLength(20)]
    public string? Status { get; set; }
}
