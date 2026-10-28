using CampusConnect.Data;
using CampusConnect.Models;
using CampusConnect.Services;
using CampusConnect.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CampusConnect.Controllers;

[Authorize(Roles = "Organizer,Admin")]
public class OrganizerController : Controller
{
    private readonly IEventService _eventService;
    private readonly IDashboardService _dashboardService;
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<OrganizerController> _logger;

    public OrganizerController(
        IEventService eventService,
        IDashboardService dashboardService,
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        ILogger<OrganizerController> logger)
    {
        _eventService = eventService;
        _dashboardService = dashboardService;
        _context = context;
        _userManager = userManager;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Dashboard()
    {
        var organizerId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(organizerId))
        {
            return Challenge();
        }

        var model = await _dashboardService.GetOrganizerDashboardAsync(organizerId);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> MyEvents()
    {
        var organizerId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(organizerId))
        {
            return Challenge();
        }

        var events = await _eventService.GetOrganizerEventsAsync(organizerId);
        return View(events);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
        var model = new EventCreateEditViewModel
        {
            StartDateTime = DateTime.UtcNow.AddDays(7).Date.AddHours(10),
            EndDateTime = DateTime.UtcNow.AddDays(7).Date.AddHours(16),
            RegistrationDeadline = DateTime.UtcNow.AddDays(5).Date.AddHours(23).AddMinutes(59),
            Capacity = 100,
            CategoryList = new SelectList(categories, "Id", "Name")
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EventCreateEditViewModel model)
    {
        var organizerId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(organizerId))
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            var categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
            model.CategoryList = new SelectList(categories, "Id", "Name", model.CategoryId);
            return View(model);
        }

        var newEvent = await _eventService.CreateEventAsync(model, organizerId);
        TempData["SuccessMessage"] = $"Event '{newEvent.Title}' was successfully created!";

        return RedirectToAction(nameof(MyEvents));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var ev = await _eventService.GetEventDetailsAsync(id);
        if (ev == null) return NotFound();

        var currentUserId = _userManager.GetUserId(User);
        if (!User.IsInRole("Admin") && ev.OrganizerId != currentUserId)
        {
            TempData["ErrorMessage"] = "You can only edit events that you created.";
            return RedirectToAction(nameof(MyEvents));
        }

        var categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
        var model = new EventCreateEditViewModel
        {
            Id = ev.Id,
            Title = ev.Title,
            Description = ev.Description,
            BannerUrl = ev.BannerUrl,
            StartDateTime = ev.StartDateTime,
            EndDateTime = ev.EndDateTime,
            Venue = ev.Venue,
            Capacity = ev.Capacity,
            RegistrationDeadline = ev.RegistrationDeadline,
            Eligibility = ev.Eligibility,
            CategoryId = ev.CategoryId,
            Status = ev.Status,
            CategoryList = new SelectList(categories, "Id", "Name", ev.CategoryId)
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EventCreateEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
            model.CategoryList = new SelectList(categories, "Id", "Name", model.CategoryId);
            return View(model);
        }

        var currentUserId = _userManager.GetUserId(User);
        var isAdmin = User.IsInRole("Admin");

        var updated = await _eventService.UpdateEventAsync(model, currentUserId!, isAdmin);
        if (!updated)
        {
            TempData["ErrorMessage"] = "Failed to update event. You might not have permission.";
            return RedirectToAction(nameof(MyEvents));
        }

        TempData["SuccessMessage"] = "Event details updated successfully.";
        return RedirectToAction(nameof(MyEvents));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var currentUserId = _userManager.GetUserId(User);
        var isAdmin = User.IsInRole("Admin");

        var deleted = await _eventService.DeleteEventAsync(id, currentUserId!, isAdmin);
        if (deleted)
        {
            TempData["SuccessMessage"] = "Event deleted successfully.";
        }
        else
        {
            TempData["ErrorMessage"] = "Unable to delete event or insufficient permissions.";
        }

        return RedirectToAction(nameof(MyEvents));
    }

    [HttpGet]
    public async Task<IActionResult> ManageRegistrations(
        int id, string? search, AttendanceStatus? attendance, RegistrationStatus? status)
    {
        var ev = await _eventService.GetEventDetailsAsync(id);
        if (ev == null) return NotFound();

        var currentUserId = _userManager.GetUserId(User);
        if (!User.IsInRole("Admin") && ev.OrganizerId != currentUserId)
        {
            TempData["ErrorMessage"] = "Unauthorized to manage registrations for this event.";
            return RedirectToAction(nameof(MyEvents));
        }

        var model = await _eventService.GetEventRegistrationsAsync(id, search, attendance, status);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAttendance(int registrationId, AttendanceStatus status, int eventId)
    {
        var currentUserId = _userManager.GetUserId(User);
        var isAdmin = User.IsInRole("Admin");

        var (success, message) = await _eventService.MarkAttendanceAsync(registrationId, status, currentUserId!, isAdmin);

        if (success)
        {
            TempData["SuccessMessage"] = message;
        }
        else
        {
            TempData["ErrorMessage"] = message;
        }

        return RedirectToAction(nameof(ManageRegistrations), new { id = eventId });
    }
}
