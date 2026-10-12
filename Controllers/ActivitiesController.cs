using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;

namespace AcxiomCRM.Controllers;

[Authorize]
public class ActivitiesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditService _auditService;

    public ActivitiesController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IAuditService auditService)
    {
        _context = context;
        _userManager = userManager;
        _auditService = auditService;
    }

    // GET: Activities
    public async Task<IActionResult> Index(string? type, string? status, int page = 1)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isSalesExec = User.IsInRole("SalesExecutive");

        IQueryable<Activity> query = _context.Activities
            .Include(a => a.Customer)
            .Include(a => a.Lead);

        if (isSalesExec && !string.IsNullOrEmpty(currentUserId))
        {
            query = query.Where(a => a.AssignedTo == currentUserId);
        }

        if (!string.IsNullOrWhiteSpace(type))
        {
            query = query.Where(a => a.ActivityType == type);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(a => a.Status == status);
        }

        ViewBag.Type = type;
        ViewBag.Status = status;

        const int pageSize = 10;
        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        page = Math.Max(1, Math.Min(page, Math.Max(1, totalPages)));

        var activities = await query
            .OrderByDescending(a => a.ActivityDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalCount = totalItems;

        var users = await _userManager.Users.ToDictionaryAsync(u => u.Id, u => u.FullName);
        ViewBag.UsersLookup = users;

        return View(activities);
    }

    // GET: Activities/Create
    public async Task<IActionResult> Create(int? customerId = null, int? leadId = null)
    {
        await PopulateDropdownsAsync(customerId, leadId);
        return View(new ActivityInputModel
        {
            CustomerId = customerId,
            LeadId = leadId,
            ActivityDate = DateTime.Now
        });
    }

    // POST: Activities/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ActivityInputModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync(model.CustomerId, model.LeadId, model.AssignedTo);
            return View(model);
        }

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var activity = new Activity
        {
            ActivityType = model.ActivityType,
            Subject = model.Subject.Trim(),
            Description = model.Description?.Trim(),
            ActivityDate = model.ActivityDate,
            CustomerId = model.CustomerId,
            LeadId = model.LeadId,
            Status = model.Status,
            AssignedTo = User.IsInRole("SalesExecutive") ? currentUserId : model.AssignedTo
        };

        _context.Activities.Add(activity);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync("Create", "Activity", activity.ActivityId.ToString(), null,
            $"Logged Activity: Type: {activity.ActivityType}, Subject: {activity.Subject}");

        TempData["SuccessMessage"] = "Activity logged successfully.";
        return RedirectToAction(nameof(Index));
    }

    // POST: Activities/MarkComplete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkComplete(int id)
    {
        var activity = await _context.Activities.FindAsync(id);
        if (activity == null)
        {
            return NotFound();
        }

        activity.Status = "Completed";
        _context.Update(activity);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync("Update", "Activity", id.ToString(), "Status: Planned", "Status: Completed");

        TempData["SuccessMessage"] = "Activity marked as completed.";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateDropdownsAsync(int? customerId = null, int? leadId = null, string? assignedTo = null)
    {
        var customers = await _context.Customers.Where(c => c.Status == "Active").ToListAsync();
        ViewBag.CustomersList = new SelectList(customers, "CustomerId", "CustomerName", customerId);

        var leads = await _context.Leads.Where(l => l.Status != "Lost").ToListAsync();
        ViewBag.LeadsList = new SelectList(leads, "LeadId", "LeadName", leadId);

        var users = await _userManager.Users.Where(u => u.IsActive).ToListAsync();
        ViewBag.UsersList = new SelectList(users, "Id", "FullName", assignedTo);
    }
}
