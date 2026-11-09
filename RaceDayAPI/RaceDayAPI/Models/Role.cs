namespace RaceDayAPI.Models;

public class Role
{
    public int RoleID { get; set; }
    public string RoleName { get; set; } = string.Empty;

    public ICollection<AppUser> Users { get; set; } = new List<AppUser>();
}
