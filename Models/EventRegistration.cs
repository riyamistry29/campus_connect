using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusConnect.Models;

public class EventRegistration
{
    public int Id { get; set; }

    [Required]
    public int EventId { get; set; }

    [ForeignKey(nameof(EventId))]
    public virtual CollegeEvent Event { get; set; } = null!;

    [Required]
    public string StudentId { get; set; } = string.Empty;

    [ForeignKey(nameof(StudentId))]
    public virtual ApplicationUser Student { get; set; } = null!;

    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

    public RegistrationStatus Status { get; set; } = RegistrationStatus.Confirmed;

    public AttendanceStatus AttendanceStatus { get; set; } = AttendanceStatus.NotMarked;

    public DateTime? AttendanceMarkedAt { get; set; }

    [StringLength(200)]
    public string? Remarks { get; set; }
}
