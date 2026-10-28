using CampusConnect.Data;
using CampusConnect.Models;
using CampusConnect.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CampusConnect.Services;

public class DashboardService : IDashboardService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public DashboardService(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<StudentDashboardViewModel> GetStudentDashboardAsync(string studentId)
    {
        var student = await _userManager.FindByIdAsync(studentId);
        if (student == null) throw new InvalidOperationException("Student not found");

        var now = DateTime.UtcNow;

        var registrations = await _context.Registrations
            .Include(r => r.Event)
                .ThenInclude(e => e.Category)
            .Include(r => r.Event)
                .ThenInclude(e => e.Organizer)
            .Where(r => r.StudentId == studentId && r.Status == RegistrationStatus.Confirmed)
            .OrderByDescending(r => r.RegisteredAt)
            .ToListAsync();

        var totalRegistered = registrations.Count;
        var upcomingEventsCount = registrations.Count(r => r.Event.EndDateTime >= now);
        var pastEventsCount = registrations.Count(r => r.Event.EndDateTime < now);
        var attendedEventsCount = registrations.Count(r => r.AttendanceStatus == AttendanceStatus.Present);

        // Fetch 3 recommended upcoming published events the student has not yet registered for
        var registeredEventIds = registrations.Select(r => r.EventId).ToHashSet();
        var recommended = await _context.Events
            .Include(e => e.Category)
            .Include(e => e.Organizer)
            .Include(e => e.Registrations)
            .Where(e => e.Status == EventStatus.Published &&
                        e.EndDateTime >= now &&
                        !registeredEventIds.Contains(e.Id))
            .OrderBy(e => e.StartDateTime)
            .Take(3)
            .ToListAsync();

        return new StudentDashboardViewModel
        {
            Student = student,
            TotalRegistered = totalRegistered,
            UpcomingEventsCount = upcomingEventsCount,
            PastEventsCount = pastEventsCount,
            AttendedEventsCount = attendedEventsCount,
            RecentRegistrations = registrations.Take(5).ToList(),
            RecommendedEvents = recommended
        };
    }

    public async Task<OrganizerDashboardViewModel> GetOrganizerDashboardAsync(string organizerId)
    {
        var organizer = await _userManager.FindByIdAsync(organizerId);
        if (organizer == null) throw new InvalidOperationException("Organizer not found");

        var events = await _context.Events
            .Include(e => e.Category)
            .Include(e => e.Registrations)
                .ThenInclude(r => r.Student)
            .Where(e => e.OrganizerId == organizerId)
            .OrderByDescending(e => e.StartDateTime)
            .ToListAsync();

        var totalEvents = events.Count;
        var publishedEvents = events.Count(e => e.Status == EventStatus.Published);
        var draftOrPending = events.Count(e => e.Status == EventStatus.Draft || e.Status == EventStatus.PendingApproval);

        var allRegistrations = events.SelectMany(e => e.Registrations).ToList();
        var confirmedRegistrations = allRegistrations.Where(r => r.Status == RegistrationStatus.Confirmed).ToList();
        var totalAttended = confirmedRegistrations.Count(r => r.AttendanceStatus == AttendanceStatus.Present);

        var recentRegistrations = confirmedRegistrations
            .OrderByDescending(r => r.RegisteredAt)
            .Take(6)
            .ToList();

        // Chart Data: Top 5 events by registrations vs capacity
        var topEvents = events.Take(6).ToList();
        var eventLabels = topEvents.Select(e => e.Title.Length > 20 ? e.Title.Substring(0, 18) + ".." : e.Title).ToList();
        var eventRegistrationCounts = topEvents.Select(e => e.Registrations.Count(r => r.Status == RegistrationStatus.Confirmed)).ToList();
        var eventCapacityLimits = topEvents.Select(e => e.Capacity).ToList();

        return new OrganizerDashboardViewModel
        {
            Organizer = organizer,
            TotalEvents = totalEvents,
            PublishedEvents = publishedEvents,
            DraftOrPendingEvents = draftOrPending,
            TotalRegistrations = confirmedRegistrations.Count,
            TotalAttended = totalAttended,
            Events = events,
            RecentRegistrations = recentRegistrations,
            EventLabels = eventLabels,
            EventRegistrationCounts = eventRegistrationCounts,
            EventCapacityLimits = eventCapacityLimits
        };
    }

    public async Task<AdminDashboardViewModel> GetAdminDashboardAsync()
    {
        var students = await _userManager.GetUsersInRoleAsync("Student");
        var organizers = await _userManager.GetUsersInRoleAsync("Organizer");

        var events = await _context.Events
            .Include(e => e.Category)
            .Include(e => e.Organizer)
            .Include(e => e.Registrations)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();

        var totalEvents = events.Count;
        var publishedEvents = events.Count(e => e.Status == EventStatus.Published);
        var pendingApproval = events.Count(e => e.Status == EventStatus.PendingApproval);

        var allConfirmedRegistrations = await _context.Registrations
            .Where(r => r.Status == RegistrationStatus.Confirmed)
            .ToListAsync();

        var totalRegistrations = allConfirmedRegistrations.Count;
        var totalAttended = allConfirmedRegistrations.Count(r => r.AttendanceStatus == AttendanceStatus.Present);

        var pendingEventsList = events.Where(e => e.Status == EventStatus.PendingApproval).ToList();
        var recentEventsList = events.Take(5).ToList();
        var recentUsersList = await _context.Users
            .OrderByDescending(u => u.CreatedAt)
            .Take(5)
            .ToListAsync();

        // Category breakdown for Pie Chart
        var categories = await _context.Categories
            .Include(c => c.Events)
            .ToListAsync();

        var categoryLabels = categories.Select(c => c.Name).ToList();
        var categoryEventCounts = categories.Select(c => c.Events.Count).ToList();

        // Monthly trends (past 6 months)
        var months = new List<string>();
        var monthCounts = new List<int>();

        var now = DateTime.UtcNow;
        for (int i = 5; i >= 0; i--)
        {
            var targetMonth = now.AddMonths(-i);
            var monthName = targetMonth.ToString("MMM yyyy");
            var count = allConfirmedRegistrations.Count(r => r.RegisteredAt.Year == targetMonth.Year && r.RegisteredAt.Month == targetMonth.Month);
            months.Add(monthName);
            monthCounts.Add(count);
        }

        return new AdminDashboardViewModel
        {
            TotalStudents = students.Count,
            TotalOrganizers = organizers.Count,
            TotalEvents = totalEvents,
            PublishedEvents = publishedEvents,
            PendingApprovalEvents = pendingApproval,
            TotalRegistrations = totalRegistrations,
            TotalAttended = totalAttended,
            PendingEventsList = pendingEventsList,
            RecentEventsList = recentEventsList,
            RecentUsersList = recentUsersList,
            CategoryLabels = categoryLabels,
            CategoryEventCounts = categoryEventCounts,
            RegistrationMonthLabels = months,
            RegistrationMonthCounts = monthCounts
        };
    }
}
