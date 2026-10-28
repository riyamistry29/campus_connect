using CampusConnect.Models;
using CampusConnect.Services;
using CampusConnect.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CampusConnect.Controllers;

public class EventsController : Controller
{
    private readonly IEventService _eventService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<EventsController> _logger;

    public EventsController(
        IEventService eventService,
        UserManager<ApplicationUser> userManager,
        ILogger<EventsController> logger)
    {
        _eventService = eventService;
        _userManager = userManager;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? search, int? categoryId, string? timeFrame = "upcoming", string? sortBy = "date_asc", int page = 1)
    {
        var model = await _eventService.GetFilteredEventsAsync(search, categoryId, timeFrame, sortBy, page, pageSize: 9);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var ev = await _eventService.GetEventDetailsAsync(id);
        if (ev == null)
        {
            return NotFound();
        }

        var currentUserId = _userManager.GetUserId(User);
        bool isRegistered = false;
        EventRegistration? userReg = null;

        if (!string.IsNullOrEmpty(currentUserId))
        {
            isRegistered = await _eventService.IsStudentRegisteredAsync(id, currentUserId);
            userReg = await _eventService.GetStudentRegistrationAsync(id, currentUserId);
        }

        bool isOrganizerOrAdmin = false;
        if (User.Identity?.IsAuthenticated == true)
        {
            isOrganizerOrAdmin = User.IsInRole("Admin") || (User.IsInRole("Organizer") && ev.OrganizerId == currentUserId);
        }

        // Determine if user can register
        bool canRegister = false;
        string? restrictionReason = null;

        if (!User.Identity?.IsAuthenticated == true)
        {
            canRegister = false;
            restrictionReason = "Please log in as a student to register for this event.";
        }
        else if (User.IsInRole("Admin") || User.IsInRole("Organizer"))
        {
            canRegister = false;
            restrictionReason = "Only registered students can enroll as attendees.";
        }
        else if (ev.Status != EventStatus.Published)
        {
            canRegister = false;
            restrictionReason = "This event is currently not open for registration.";
        }
        else if (DateTime.UtcNow > ev.RegistrationDeadline)
        {
            canRegister = false;
            restrictionReason = "The registration deadline for this event has passed.";
        }
        else if (ev.IsFull && !isRegistered)
        {
            canRegister = false;
            restrictionReason = "This event has reached its maximum seat capacity.";
        }
        else
        {
            canRegister = true;
        }

        var viewModel = new EventDetailsViewModel
        {
            Event = ev,
            IsUserRegistered = isRegistered,
            UserRegistration = userReg,
            CanRegister = canRegister,
            RegistrationRestrictionReason = restrictionReason,
            IsOrganizerOrAdmin = isOrganizerOrAdmin,
            Announcements = ev.Announcements.OrderByDescending(a => a.CreatedAt).ToList()
        };

        return View(viewModel);
    }

    [HttpPost]
    [Authorize(Roles = "Student")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(int id)
    {
        var studentId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(studentId))
        {
            return Challenge();
        }

        var (success, message) = await _eventService.RegisterStudentAsync(id, studentId);

        if (success)
        {
            TempData["SuccessMessage"] = message;
        }
        else
        {
            TempData["ErrorMessage"] = message;
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [Authorize(Roles = "Student")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var studentId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(studentId))
        {
            return Challenge();
        }

        var (success, message) = await _eventService.CancelRegistrationAsync(id, studentId);

        if (success)
        {
            TempData["SuccessMessage"] = message;
        }
        else
        {
            TempData["ErrorMessage"] = message;
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [Authorize(Roles = "Organizer,Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddAnnouncement(int eventId, string title, string message)
    {
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(message))
        {
            TempData["ErrorMessage"] = "Announcement title and message cannot be empty.";
            return RedirectToAction(nameof(Details), new { id = eventId });
        }

        var userId = _userManager.GetUserId(User);
        var (success, resultMessage) = await _eventService.AddAnnouncementAsync(eventId, title, message, userId!);

        if (success)
        {
            TempData["SuccessMessage"] = resultMessage;
        }
        else
        {
            TempData["ErrorMessage"] = resultMessage;
        }

        return RedirectToAction(nameof(Details), new { id = eventId });
    }
}
