using CampusConnect.Data;
using CampusConnect.Models;
using CampusConnect.Services;
using CampusConnect.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusConnect.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly IDashboardService _dashboardService;
    private readonly IEventService _eventService;
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        IDashboardService dashboardService,
        IEventService eventService,
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        ILogger<AdminController> logger)
    {
        _dashboardService = dashboardService;
        _eventService = eventService;
        _context = context;
        _userManager = userManager;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Dashboard()
    {
        var model = await _dashboardService.GetAdminDashboardAsync();
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> ModerateEvents(EventStatus? statusFilter = null)
    {
        var query = _context.Events
            .Include(e => e.Category)
            .Include(e => e.Organizer)
            .Include(e => e.Registrations)
            .AsQueryable();

        if (statusFilter.HasValue)
        {
            query = query.Where(e => e.Status == statusFilter.Value);
        }

        var events = await query.OrderByDescending(e => e.CreatedAt).ToListAsync();
        ViewBag.CurrentFilter = statusFilter;
        return View(events);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeStatus(int id, EventStatus status)
    {
        var (success, message) = await _eventService.ModerateEventStatusAsync(id, status);
        if (success)
        {
            TempData["SuccessMessage"] = message;
        }
        else
        {
            TempData["ErrorMessage"] = message;
        }

        return RedirectToAction(nameof(ModerateEvents));
    }

    [HttpGet]
    public async Task<IActionResult> Categories()
    {
        var categories = await _context.Categories
            .Include(c => c.Events)
            .OrderBy(c => c.Name)
            .ToListAsync();

        return View(categories);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCategory(string name, string? description, string? iconClass)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["ErrorMessage"] = "Category name cannot be empty.";
            return RedirectToAction(nameof(Categories));
        }

        if (await _context.Categories.AnyAsync(c => c.Name.ToLower() == name.Trim().ToLower()))
        {
            TempData["ErrorMessage"] = "A category with this name already exists.";
            return RedirectToAction(nameof(Categories));
        }

        var cat = new EventCategory
        {
            Name = name.Trim(),
            Description = description?.Trim(),
            IconClass = string.IsNullOrWhiteSpace(iconClass) ? "bi-calendar-event" : iconClass.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _context.Categories.Add(cat);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Category '{cat.Name}' created successfully.";
        return RedirectToAction(nameof(Categories));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var cat = await _context.Categories
            .Include(c => c.Events)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (cat == null) return NotFound();

        if (cat.Events.Any())
        {
            TempData["ErrorMessage"] = $"Cannot delete category '{cat.Name}' because it is linked to {cat.Events.Count} event(s). Please reassign or delete those events first.";
            return RedirectToAction(nameof(Categories));
        }

        _context.Categories.Remove(cat);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Category '{cat.Name}' deleted successfully.";
        return RedirectToAction(nameof(Categories));
    }

    [HttpGet]
    public async Task<IActionResult> Users()
    {
        var users = await _context.Users
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync();

        var viewModels = new List<UserRoleManagementViewModel>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            viewModels.Add(new UserRoleManagementViewModel
            {
                User = user,
                Roles = roles
            });
        }

        return View(viewModels);
    }

    [HttpGet]
    public async Task<IActionResult> AllRegistrations(string? search, RegistrationStatus? statusFilter)
    {
        var query = _context.Registrations
            .Include(r => r.Student)
            .Include(r => r.Event)
                .ThenInclude(e => e.Category)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(r => r.Student.FullName.ToLower().Contains(s) ||
                                     r.Student.Email!.ToLower().Contains(s) ||
                                     r.Event.Title.ToLower().Contains(s));
        }

        if (statusFilter.HasValue)
        {
            query = query.Where(r => r.Status == statusFilter.Value);
        }

        var list = await query.OrderByDescending(r => r.RegisteredAt).ToListAsync();
        ViewBag.Search = search;
        ViewBag.StatusFilter = statusFilter;
        return View(list);
    }
}
