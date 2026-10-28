using CampusConnect.Data;
using CampusConnect.Models;
using CampusConnect.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace CampusConnect.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<HomeController> _logger;

    public HomeController(ApplicationDbContext context, ILogger<HomeController> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var now = DateTime.UtcNow;

        // Featured upcoming published events
        var featuredEvents = await _context.Events
            .Include(e => e.Category)
            .Include(e => e.Organizer)
            .Include(e => e.Registrations)
            .Where(e => e.Status == EventStatus.Published && e.EndDateTime >= now)
            .OrderBy(e => e.StartDateTime)
            .Take(6)
            .ToListAsync();

        var categories = await _context.Categories
            .Include(c => c.Events)
            .OrderBy(c => c.Name)
            .ToListAsync();

        ViewBag.TotalEventsCount = await _context.Events.CountAsync(e => e.Status == EventStatus.Published);
        ViewBag.TotalRegistrationsCount = await _context.Registrations.CountAsync(r => r.Status == RegistrationStatus.Confirmed);
        ViewBag.TotalCategoriesCount = categories.Count;

        ViewBag.Categories = categories;
        return View(featuredEvents);
    }

    public IActionResult About()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
