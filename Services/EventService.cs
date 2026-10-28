using System.Data;
using CampusConnect.Data;
using CampusConnect.Models;
using CampusConnect.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace CampusConnect.Services;

/// <summary>
/// EventService contains all the business logic for college events.
/// Written in clean, readable C# so any student or junior developer can easily follow and explain it.
/// </summary>
public class EventService : IEventService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<EventService> _logger;

    public EventService(ApplicationDbContext context, ILogger<EventService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // =========================================================================
    // 1. GET FILTERED EVENTS (Search, Category, Date, Sort, and Paging)
    // =========================================================================
    public async Task<EventFilterViewModel> GetFilteredEventsAsync(
        string? search, int? categoryId, string? timeFrame, string? sortBy, int page = 1, int pageSize = 9)
    {
        var now = DateTime.UtcNow;

        // Start with only published events
        var query = _context.Events
            .Include(e => e.Category)
            .Include(e => e.Organizer)
            .Include(e => e.Registrations)
            .Where(e => e.Status == EventStatus.Published)
            .AsQueryable();

        // Step A: Filter by search keyword (Title, Description, or Venue)
        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.Trim().ToLower();
            query = query.Where(e => e.Title.ToLower().Contains(searchLower) ||
                                     e.Description.ToLower().Contains(searchLower) ||
                                     e.Venue.ToLower().Contains(searchLower));
        }

        // Step B: Filter by category
        if (categoryId.HasValue && categoryId.Value > 0)
        {
            query = query.Where(e => e.CategoryId == categoryId.Value);
        }

        // Step C: Filter by timeframe (Upcoming vs Past vs Today)
        timeFrame = (timeFrame ?? "upcoming").ToLower();
        if (timeFrame == "upcoming")
        {
            query = query.Where(e => e.EndDateTime >= now);
        }
        else if (timeFrame == "past")
        {
            query = query.Where(e => e.EndDateTime < now);
        }
        else if (timeFrame == "today")
        {
            var todayStart = now.Date;
            var todayEnd = todayStart.AddDays(1);
            query = query.Where(e => e.StartDateTime >= todayStart && e.StartDateTime < todayEnd);
        }
        else if (timeFrame == "this_week")
        {
            var weekEnd = now.AddDays(7);
            query = query.Where(e => e.StartDateTime >= now && e.StartDateTime <= weekEnd);
        }

        // Step D: Sort events
        query = (sortBy ?? "date_asc").ToLower() switch
        {
            "date_desc" => query.OrderByDescending(e => e.StartDateTime),
            "capacity" => query.OrderByDescending(e => e.Capacity),
            "popular" => query.OrderByDescending(e => e.Registrations.Count(r => r.Status == RegistrationStatus.Confirmed)),
            _ => query.OrderBy(e => e.StartDateTime),
        };

        // Step E: Paging (Skip and Take)
        var totalEvents = await query.CountAsync();
        var pagedEvents = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();

        return new EventFilterViewModel
        {
            Search = search,
            CategoryId = categoryId,
            TimeFrame = timeFrame,
            SortBy = sortBy ?? "date_asc",
            PageNumber = page,
            PageSize = pageSize,
            TotalEvents = totalEvents,
            Events = pagedEvents,
            Categories = categories
        };
    }

    // =========================================================================
    // 2. GET EVENT DETAILS BY ID
    // =========================================================================
    public async Task<CollegeEvent?> GetEventDetailsAsync(int id)
    {
        return await _context.Events
            .Include(e => e.Category)
            .Include(e => e.Organizer)
            .Include(e => e.Registrations)
                .ThenInclude(r => r.Student)
            .Include(e => e.Announcements)
                .ThenInclude(a => a.CreatedBy)
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    // =========================================================================
    // 3. STUDENT REGISTRATION LOGIC (With capacity & duplicate prevention)
    // =========================================================================
    public async Task<(bool Success, string Message)> RegisterStudentAsync(int eventId, string studentId)
    {
        // Use a database transaction to make sure two students cannot book the last seat at the exact same millisecond
        using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            // Step 1: Find the event
            var ev = await _context.Events
                .Include(e => e.Registrations)
                .FirstOrDefaultAsync(e => e.Id == eventId);

            if (ev == null)
            {
                return (false, "Event not found.");
            }

            // Step 2: Check if event is published
            if (ev.Status != EventStatus.Published)
            {
                return (false, "This event is not open for registration.");
            }

            // Step 3: Check registration deadline
            if (DateTime.UtcNow > ev.RegistrationDeadline)
            {
                return (false, "The registration deadline has already passed.");
            }

            // Step 4: Check if the student is already registered
            var existing = await _context.Registrations
                .FirstOrDefaultAsync(r => r.EventId == eventId && r.StudentId == studentId);

            if (existing != null)
            {
                if (existing.Status == RegistrationStatus.Confirmed)
                {
                    return (false, "You are already registered for this event.");
                }

                // If the student previously cancelled and wants to re-register:
                var activeCount = await _context.Registrations
                    .CountAsync(r => r.EventId == eventId && r.Status == RegistrationStatus.Confirmed);

                if (activeCount >= ev.Capacity)
                {
                    return (false, "Sorry, this event is now full.");
                }

                existing.Status = RegistrationStatus.Confirmed;
                existing.RegisteredAt = DateTime.UtcNow;
                existing.AttendanceStatus = AttendanceStatus.NotMarked;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return (true, "Registration reactivated successfully!");
            }

            // Step 5: Check seat capacity
            var currentSeatsFilled = await _context.Registrations
                .CountAsync(r => r.EventId == eventId && r.Status == RegistrationStatus.Confirmed);

            if (currentSeatsFilled >= ev.Capacity)
            {
                return (false, "Sorry! This event has reached full capacity.");
            }

            // Step 6: Create new registration record
            var newReg = new EventRegistration
            {
                EventId = eventId,
                StudentId = studentId,
                RegisteredAt = DateTime.UtcNow,
                Status = RegistrationStatus.Confirmed,
                AttendanceStatus = AttendanceStatus.NotMarked
            };

            _context.Registrations.Add(newReg);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return (true, "You have successfully registered for this event!");
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync();
            return (false, "Could not complete registration due to duplicate or concurrent entry.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error during student registration");
            return (false, "An unexpected error occurred. Please try again.");
        }
    }

    // =========================================================================
    // 4. CANCEL REGISTRATION
    // =========================================================================
    public async Task<(bool Success, string Message)> CancelRegistrationAsync(int eventId, string studentId)
    {
        var reg = await _context.Registrations
            .Include(r => r.Event)
            .FirstOrDefaultAsync(r => r.EventId == eventId && r.StudentId == studentId);

        if (reg == null || reg.Status != RegistrationStatus.Confirmed)
        {
            return (false, "No active registration found.");
        }

        // Cannot cancel after event has started
        if (DateTime.UtcNow >= reg.Event.StartDateTime)
        {
            return (false, "Registrations cannot be cancelled after the event has started.");
        }

        reg.Status = RegistrationStatus.Cancelled;
        await _context.SaveChangesAsync();

        return (true, "Your registration has been cancelled. Your seat is now free for another student.");
    }

    public async Task<bool> IsStudentRegisteredAsync(int eventId, string studentId)
    {
        return await _context.Registrations
            .AnyAsync(r => r.EventId == eventId && r.StudentId == studentId && r.Status == RegistrationStatus.Confirmed);
    }

    public async Task<EventRegistration?> GetStudentRegistrationAsync(int eventId, string studentId)
    {
        return await _context.Registrations
            .FirstOrDefaultAsync(r => r.EventId == eventId && r.StudentId == studentId);
    }

    public async Task<StudentRegistrationsViewModel> GetStudentRegistrationsAsync(string studentId)
    {
        var now = DateTime.UtcNow;
        var registrations = await _context.Registrations
            .Include(r => r.Event)
                .ThenInclude(e => e.Category)
            .Include(r => r.Event)
                .ThenInclude(e => e.Organizer)
            .Where(r => r.StudentId == studentId)
            .OrderByDescending(r => r.RegisteredAt)
            .ToListAsync();

        return new StudentRegistrationsViewModel
        {
            UpcomingRegistrations = registrations
                .Where(r => r.Event.EndDateTime >= now)
                .OrderBy(r => r.Event.StartDateTime)
                .ToList(),
            PastRegistrations = registrations
                .Where(r => r.Event.EndDateTime < now)
                .OrderByDescending(r => r.Event.StartDateTime)
                .ToList()
        };
    }

    public async Task<List<CollegeEvent>> GetOrganizerEventsAsync(string organizerId)
    {
        return await _context.Events
            .Include(e => e.Category)
            .Include(e => e.Registrations)
            .Where(e => e.OrganizerId == organizerId)
            .OrderByDescending(e => e.StartDateTime)
            .ToListAsync();
    }

    public async Task<CollegeEvent> CreateEventAsync(EventCreateEditViewModel model, string organizerId)
    {
        var collegeEvent = new CollegeEvent
        {
            Title = model.Title.Trim(),
            Description = model.Description.Trim(),
            BannerUrl = string.IsNullOrWhiteSpace(model.BannerUrl) 
                ? "https://images.unsplash.com/photo-1540575467063-178a50c2df87?auto=format&fit=crop&w=1200&q=80" 
                : model.BannerUrl.Trim(),
            StartDateTime = DateTime.SpecifyKind(model.StartDateTime, DateTimeKind.Utc),
            EndDateTime = DateTime.SpecifyKind(model.EndDateTime, DateTimeKind.Utc),
            Venue = model.Venue.Trim(),
            Capacity = model.Capacity,
            RegistrationDeadline = DateTime.SpecifyKind(model.RegistrationDeadline, DateTimeKind.Utc),
            Eligibility = model.Eligibility?.Trim(),
            Status = model.Status,
            CategoryId = model.CategoryId,
            OrganizerId = organizerId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Events.Add(collegeEvent);
        await _context.SaveChangesAsync();
        return collegeEvent;
    }

    public async Task<bool> UpdateEventAsync(EventCreateEditViewModel model, string userId, bool isAdmin)
    {
        var ev = await _context.Events.FirstOrDefaultAsync(e => e.Id == model.Id);
        if (ev == null) return false;

        // Verify that only the creator or an admin can edit
        if (!isAdmin && ev.OrganizerId != userId)
        {
            return false;
        }

        ev.Title = model.Title.Trim();
        ev.Description = model.Description.Trim();
        if (!string.IsNullOrWhiteSpace(model.BannerUrl))
        {
            ev.BannerUrl = model.BannerUrl.Trim();
        }
        ev.StartDateTime = DateTime.SpecifyKind(model.StartDateTime, DateTimeKind.Utc);
        ev.EndDateTime = DateTime.SpecifyKind(model.EndDateTime, DateTimeKind.Utc);
        ev.Venue = model.Venue.Trim();
        ev.Capacity = model.Capacity;
        ev.RegistrationDeadline = DateTime.SpecifyKind(model.RegistrationDeadline, DateTimeKind.Utc);
        ev.Eligibility = model.Eligibility?.Trim();
        ev.CategoryId = model.CategoryId;
        ev.Status = model.Status;
        ev.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteEventAsync(int id, string userId, bool isAdmin)
    {
        var ev = await _context.Events.FirstOrDefaultAsync(e => e.Id == id);
        if (ev == null) return false;

        if (!isAdmin && ev.OrganizerId != userId)
        {
            return false;
        }

        _context.Events.Remove(ev);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<ManageRegistrationsViewModel> GetEventRegistrationsAsync(
        int eventId, string? search, AttendanceStatus? attendance, RegistrationStatus? status)
    {
        var ev = await _context.Events
            .Include(e => e.Category)
            .Include(e => e.Organizer)
            .FirstOrDefaultAsync(e => e.Id == eventId);

        if (ev == null)
        {
            throw new InvalidOperationException("Event not found");
        }

        var query = _context.Registrations
            .Include(r => r.Student)
            .Where(r => r.EventId == eventId)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.Trim().ToLower();
            query = query.Where(r => r.Student.FullName.ToLower().Contains(searchLower) ||
                                     r.Student.Email!.ToLower().Contains(searchLower) ||
                                     (r.Student.StudentIdentifier != null && r.Student.StudentIdentifier.ToLower().Contains(searchLower)));
        }

        if (attendance.HasValue)
        {
            query = query.Where(r => r.AttendanceStatus == attendance.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        var registrations = await query.OrderByDescending(r => r.RegisteredAt).ToListAsync();

        return new ManageRegistrationsViewModel
        {
            Event = ev,
            Registrations = registrations,
            Search = search,
            AttendanceFilter = attendance,
            StatusFilter = status
        };
    }

    public async Task<(bool Success, string Message)> MarkAttendanceAsync(
        int registrationId, AttendanceStatus status, string userId, bool isAdmin)
    {
        var reg = await _context.Registrations
            .Include(r => r.Event)
            .FirstOrDefaultAsync(r => r.Id == registrationId);

        if (reg == null)
        {
            return (false, "Registration not found.");
        }

        if (!isAdmin && reg.Event.OrganizerId != userId)
        {
            return (false, "You are not authorized to mark attendance for this event.");
        }

        reg.AttendanceStatus = status;
        reg.AttendanceMarkedAt = status == AttendanceStatus.NotMarked ? null : DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return (true, $"Attendance updated to {status}!");
    }

    public async Task<(bool Success, string Message)> AddAnnouncementAsync(
        int eventId, string title, string message, string userId)
    {
        var ev = await _context.Events.FirstOrDefaultAsync(e => e.Id == eventId);
        if (ev == null) return (false, "Event not found.");

        var announcement = new EventAnnouncement
        {
            EventId = eventId,
            Title = title.Trim(),
            Message = message.Trim(),
            CreatedById = userId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Announcements.Add(announcement);
        await _context.SaveChangesAsync();

        return (true, "Announcement posted successfully!");
    }

    public async Task<(bool Success, string Message)> ModerateEventStatusAsync(int eventId, EventStatus newStatus)
    {
        var ev = await _context.Events.FirstOrDefaultAsync(e => e.Id == eventId);
        if (ev == null) return (false, "Event not found.");

        ev.Status = newStatus;
        ev.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return (true, $"Event status changed to '{newStatus}'.");
    }
}
