using CampusConnect.Models;
using CampusConnect.ViewModels;

namespace CampusConnect.Services;

public interface IEventService
{
    Task<EventFilterViewModel> GetFilteredEventsAsync(string? search, int? categoryId, string? timeFrame, string? sortBy, int page = 1, int pageSize = 9);
    Task<CollegeEvent?> GetEventDetailsAsync(int id);
    Task<(bool Success, string Message)> RegisterStudentAsync(int eventId, string studentId);
    Task<(bool Success, string Message)> CancelRegistrationAsync(int eventId, string studentId);
    Task<bool> IsStudentRegisteredAsync(int eventId, string studentId);
    Task<EventRegistration?> GetStudentRegistrationAsync(int eventId, string studentId);
    Task<StudentRegistrationsViewModel> GetStudentRegistrationsAsync(string studentId);
    
    // Organizer & Admin methods
    Task<List<CollegeEvent>> GetOrganizerEventsAsync(string organizerId);
    Task<CollegeEvent> CreateEventAsync(EventCreateEditViewModel model, string organizerId);
    Task<bool> UpdateEventAsync(EventCreateEditViewModel model, string userId, bool isAdmin);
    Task<bool> DeleteEventAsync(int id, string userId, bool isAdmin);
    Task<ManageRegistrationsViewModel> GetEventRegistrationsAsync(int eventId, string? search, AttendanceStatus? attendance, RegistrationStatus? status);
    Task<(bool Success, string Message)> MarkAttendanceAsync(int registrationId, AttendanceStatus status, string userId, bool isAdmin);
    Task<(bool Success, string Message)> AddAnnouncementAsync(int eventId, string title, string message, string userId);
    Task<(bool Success, string Message)> ModerateEventStatusAsync(int eventId, EventStatus newStatus);
}
