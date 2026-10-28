using System.ComponentModel.DataAnnotations;

namespace CampusConnect.Models;

public class EventCategory
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [StringLength(50)]
    public string? IconClass { get; set; } = "bi-calendar-event";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public virtual ICollection<CollegeEvent> Events { get; set; } = new List<CollegeEvent>();
}
