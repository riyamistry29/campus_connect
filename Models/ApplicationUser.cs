using Microsoft.AspNetCore.Identity;

namespace CampusConnect.Models;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string? StudentIdentifier { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual ICollection<CollegeEvent> OrganizedEvents { get; set; } = new List<CollegeEvent>();
    public virtual ICollection<EventRegistration> Registrations { get; set; } = new List<EventRegistration>();
}
