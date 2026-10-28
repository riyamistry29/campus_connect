using CampusConnect.Models;
using CampusConnect.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CampusConnect.Controllers;

[Authorize(Roles = "Student")]
public class RegistrationsController : Controller
{
    private readonly IEventService _eventService;
    private readonly IDashboardService _dashboardService;
    private readonly UserManager<ApplicationUser> _userManager;

    public RegistrationsController(
        IEventService eventService,
        IDashboardService dashboardService,
        UserManager<ApplicationUser> userManager)
    {
        _eventService = eventService;
        _dashboardService = dashboardService;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Dashboard()
    {
        var studentId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(studentId))
        {
            return Challenge();
        }

        var model = await _dashboardService.GetStudentDashboardAsync(studentId);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var studentId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(studentId))
        {
            return Challenge();
        }

        var model = await _eventService.GetStudentRegistrationsAsync(studentId);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelRegistration(int eventId)
    {
        var studentId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(studentId))
        {
            return Challenge();
        }

        var (success, message) = await _eventService.CancelRegistrationAsync(eventId, studentId);
        if (success)
        {
            TempData["SuccessMessage"] = message;
        }
        else
        {
            TempData["ErrorMessage"] = message;
        }

        return RedirectToAction(nameof(Index));
    }
}
