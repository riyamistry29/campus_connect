using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusConnect.Models;

public class CollegeEvent
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    [StringLength(500)]
    public string? BannerUrl { get; set; }

    [Required]
    public DateTime StartDateTime { get; set; }

    [Required]
    public DateTime EndDateTime { get; set; }

    [Required]
    [StringLength(200)]
    public string Venue { get; set; } = string.Empty;

    [Required]
    [Range(1, 10000, ErrorMessage = "Capacity must be between 1 and 10,000")]
    public int Capacity { get; set; }

    [Required]
    public DateTime RegistrationDeadline { get; set; }

    [StringLength(300)]
    public string? Eligibility { get; set; } = "Open to all enrolled students";

    public EventStatus Status { get; set; } = EventStatus.Draft;

    // Foreign Keys
    [Required]
    public int CategoryId { get; set; }

    [ForeignKey(nameof(CategoryId))]
    public virtual EventCategory Category { get; set; } = null!;

    [Required]
    public string OrganizerId { get; set; } = string.Empty;

    [ForeignKey(nameof(OrganizerId))]
    public virtual ApplicationUser Organizer { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public virtual ICollection<EventRegistration> Registrations { get; set; } = new List<EventRegistration>();
    public virtual ICollection<EventAnnouncement> Announcements { get; set; } = new List<EventAnnouncement>();

    // Helper properties (not mapped to DB)
    [NotMapped]
    public int ConfirmedRegistrationsCount => Registrations.Count(r => r.Status == RegistrationStatus.Confirmed);

    [NotMapped]
    public int AvailableSeats => Math.Max(0, Capacity - ConfirmedRegistrationsCount);

    [NotMapped]
    public bool IsFull => AvailableSeats <= 0;

    [NotMapped]
    public bool IsRegistrationOpen => Status == EventStatus.Published &&
                                     DateTime.UtcNow <= RegistrationDeadline &&
                                     !IsFull;
}
