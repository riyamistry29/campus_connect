using CampusConnect.ViewModels;

namespace CampusConnect.Services;

public interface IDashboardService
{
    Task<StudentDashboardViewModel> GetStudentDashboardAsync(string studentId);
    Task<OrganizerDashboardViewModel> GetOrganizerDashboardAsync(string organizerId);
    Task<AdminDashboardViewModel> GetAdminDashboardAsync();
}
