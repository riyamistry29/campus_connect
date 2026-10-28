using System.ComponentModel.DataAnnotations;
using CampusConnect.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CampusConnect.ViewModels;

public class EventFilterViewModel
{
    public string? Search { get; set; }
    public int? CategoryId { get; set; }
    public string? TimeFrame { get; set; } = "upcoming"; // "upcoming", "past", "all", "today", "this_week"
    public string? SortBy { get; set; } = "date_asc"; // "date_asc", "date_desc", "capacity", "popular"
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 9;

    public int TotalEvents { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalEvents / PageSize);

    public List<CollegeEvent> Events { get; set; } = new();
    public List<EventCategory> Categories { get; set; } = new();
}

public class EventCreateEditViewModel : IValidatableObject
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Event title is required")]
    [StringLength(200, MinimumLength = 5, ErrorMessage = "Title must be between 5 and 200 characters")]
    [Display(Name = "Event Title")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Event description is required")]
    [MinLength(20, ErrorMessage = "Please provide a detailed description (at least 20 characters)")]
    public string Description { get; set; } = string.Empty;

    [Display(Name = "Banner Image URL")]
    [Url(ErrorMessage = "Please provide a valid URL for the banner image")]
    public string? BannerUrl { get; set; }

    [Required(ErrorMessage = "Start date and time are required")]
    [Display(Name = "Start Date & Time")]
    public DateTime StartDateTime { get; set; } = DateTime.UtcNow.AddDays(7);

    [Required(ErrorMessage = "End date and time are required")]
    [Display(Name = "End Date & Time")]
    public DateTime EndDateTime { get; set; } = DateTime.UtcNow.AddDays(7).AddHours(3);

    [Required(ErrorMessage = "Venue / Location is required")]
    [StringLength(200, ErrorMessage = "Venue cannot exceed 200 characters")]
    [Display(Name = "Venue / Room / Campus Location")]
    public string Venue { get; set; } = string.Empty;

    [Required(ErrorMessage = "Maximum capacity is required")]
    [Range(1, 10000, ErrorMessage = "Capacity must be between 1 and 10,000 attendees")]
    [Display(Name = "Maximum Attendee Capacity")]
    public int Capacity { get; set; } = 100;

    [Required(ErrorMessage = "Registration deadline is required")]
    [Display(Name = "Registration Deadline")]
    public DateTime RegistrationDeadline { get; set; } = DateTime.UtcNow.AddDays(6);

    [StringLength(300)]
    [Display(Name = "Eligibility Requirements")]
    public string? Eligibility { get; set; } = "Open to all enrolled college students";

    [Display(Name = "Event Category")]
    [Required(ErrorMessage = "Please select an event category")]
    public int CategoryId { get; set; }

    [Display(Name = "Event Status")]
    public EventStatus Status { get; set; } = EventStatus.Published;

    public SelectList? CategoryList { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EndDateTime <= StartDateTime)
        {
            yield return new ValidationResult(
                "Event End Date & Time must be later than Start Date & Time.",
                new[] { nameof(EndDateTime) }
            );
        }

        if (RegistrationDeadline > StartDateTime)
        {
            yield return new ValidationResult(
                "Registration Deadline cannot be set after the event has started.",
                new[] { nameof(RegistrationDeadline) }
            );
        }
    }
}

public class EventDetailsViewModel
{
    public CollegeEvent Event { get; set; } = null!;
    public bool IsUserRegistered { get; set; }
    public EventRegistration? UserRegistration { get; set; }
    public bool CanRegister { get; set; }
    public string? RegistrationRestrictionReason { get; set; }
    public bool IsOrganizerOrAdmin { get; set; }
    public List<EventAnnouncement> Announcements { get; set; } = new();
}

public class StudentRegistrationsViewModel
{
    public List<EventRegistration> UpcomingRegistrations { get; set; } = new();
    public List<EventRegistration> PastRegistrations { get; set; } = new();
}

public class ManageRegistrationsViewModel
{
    public CollegeEvent Event { get; set; } = null!;
    public List<EventRegistration> Registrations { get; set; } = new();
    public string? Search { get; set; }
    public AttendanceStatus? AttendanceFilter { get; set; }
    public RegistrationStatus? StatusFilter { get; set; }
}
