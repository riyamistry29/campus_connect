using CampusConnect.Models;

namespace CampusConnect.ViewModels;

public class StudentDashboardViewModel
{
    public ApplicationUser Student { get; set; } = null!;
    public int TotalRegistered { get; set; }
    public int UpcomingEventsCount { get; set; }
    public int PastEventsCount { get; set; }
    public int AttendedEventsCount { get; set; }
    public List<EventRegistration> RecentRegistrations { get; set; } = new();
    public List<CollegeEvent> RecommendedEvents { get; set; } = new();
}

public class OrganizerDashboardViewModel
{
    public ApplicationUser Organizer { get; set; } = null!;
    public int TotalEvents { get; set; }
    public int PublishedEvents { get; set; }
    public int DraftOrPendingEvents { get; set; }
    public int TotalRegistrations { get; set; }
    public int TotalAttended { get; set; }
    public double AttendanceRate => TotalRegistrations > 0 ? Math.Round((double)TotalAttended / TotalRegistrations * 100, 1) : 0;
    public List<CollegeEvent> Events { get; set; } = new();
    public List<EventRegistration> RecentRegistrations { get; set; } = new();
    
    // Chart data: Registrations per event
    public List<string> EventLabels { get; set; } = new();
    public List<int> EventRegistrationCounts { get; set; } = new();
    public List<int> EventCapacityLimits { get; set; } = new();
}

public class AdminDashboardViewModel
{
    public int TotalStudents { get; set; }
    public int TotalOrganizers { get; set; }
    public int TotalEvents { get; set; }
    public int PublishedEvents { get; set; }
    public int PendingApprovalEvents { get; set; }
    public int TotalRegistrations { get; set; }
    public int TotalAttended { get; set; }
    public double AttendanceRate => TotalRegistrations > 0 ? Math.Round((double)TotalAttended / TotalRegistrations * 100, 1) : 0;

    public List<CollegeEvent> PendingEventsList { get; set; } = new();
    public List<CollegeEvent> RecentEventsList { get; set; } = new();
    public List<ApplicationUser> RecentUsersList { get; set; } = new();

    // Chart Data: Category Distribution
    public List<string> CategoryLabels { get; set; } = new();
    public List<int> CategoryEventCounts { get; set; } = new();

    // Chart Data: Monthly / Status Registrations
    public List<string> RegistrationMonthLabels { get; set; } = new();
    public List<int> RegistrationMonthCounts { get; set; } = new();
}

public class UserRoleManagementViewModel
{
    public ApplicationUser User { get; set; } = null!;
    public IList<string> Roles { get; set; } = new List<string>();
}
