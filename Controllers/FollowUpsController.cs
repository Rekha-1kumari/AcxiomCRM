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
public class FollowUpsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditService _auditService;

    public FollowUpsController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IAuditService auditService)
    {
        _context = context;
        _userManager = userManager;
        _auditService = auditService;
    }

    // GET: FollowUps
    public async Task<IActionResult> Index(string? status, string? type, int page = 1)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isSalesExec = User.IsInRole("SalesExecutive");

        IQueryable<FollowUp> query = _context.FollowUps
            .Include(f => f.Customer)
            .Include(f => f.Lead)
            .Include(f => f.Opportunity);

        if (isSalesExec && !string.IsNullOrEmpty(currentUserId))
        {
            query = query.Where(f => f.AssignedTo == currentUserId);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(f => f.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(type))
        {
            query = query.Where(f => f.FollowUpType == type);
        }

        ViewBag.Status = status;
        ViewBag.Type = type;

        const int pageSize = 10;
        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        page = Math.Max(1, Math.Min(page, Math.Max(1, totalPages)));

        var followUps = await query
            .OrderByDescending(f => f.FollowUpDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalCount = totalItems;

        var users = await _userManager.Users.ToDictionaryAsync(u => u.Id, u => u.FullName);
        ViewBag.UsersLookup = users;

        return View(followUps);
    }

    // GET: FollowUps/Create
    public async Task<IActionResult> Create(int? customerId = null, int? leadId = null, int? opportunityId = null)
    {
        await PopulateDropdownsAsync(customerId, leadId, opportunityId);
        return View(new FollowUpInputModel
        {
            CustomerId = customerId,
            LeadId = leadId,
            OpportunityId = opportunityId,
            FollowUpDate = DateTime.Today.AddDays(1).AddHours(10)
        });
    }

    // POST: FollowUps/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(FollowUpInputModel model)
    {
        // Business Rule: Follow-up date cannot be earlier than today for a new/planned follow-up
        if (model.Status == "Planned" && model.FollowUpDate.Date < DateTime.Today)
        {
            ModelState.AddModelError("FollowUpDate", "Follow-up date cannot be earlier than today.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync(model.CustomerId, model.LeadId, model.OpportunityId, model.AssignedTo);
            return View(model);
        }

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var followUp = new FollowUp
        {
            CustomerId = model.CustomerId,
            LeadId = model.LeadId,
            OpportunityId = model.OpportunityId,
            FollowUpDate = model.FollowUpDate,
            FollowUpType = model.FollowUpType,
            Remarks = model.Remarks.Trim(),
            Status = model.Status,
            AssignedTo = User.IsInRole("SalesExecutive") ? currentUserId : model.AssignedTo
        };

        _context.FollowUps.Add(followUp);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync("Create", "FollowUp", followUp.FollowUpId.ToString(), null,
            $"Created Follow-Up: Type: {followUp.FollowUpType}, Date: {followUp.FollowUpDate:g}, Remarks: {followUp.Remarks}");

        TempData["SuccessMessage"] = "Follow-up scheduled successfully.";
        return RedirectToAction(nameof(Index));
    }

    // GET: FollowUps/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isSalesExec = User.IsInRole("SalesExecutive");

        var followUp = await _context.FollowUps.FindAsync(id);
        if (followUp == null)
        {
            return NotFound();
        }

        if (isSalesExec && followUp.AssignedTo != currentUserId)
        {
            return Forbid();
        }

        var model = new FollowUpInputModel
        {
            FollowUpId = followUp.FollowUpId,
            CustomerId = followUp.CustomerId,
            LeadId = followUp.LeadId,
            OpportunityId = followUp.OpportunityId,
            FollowUpDate = followUp.FollowUpDate,
            FollowUpType = followUp.FollowUpType,
            Remarks = followUp.Remarks,
            Status = followUp.Status,
            AssignedTo = followUp.AssignedTo
        };

        await PopulateDropdownsAsync(followUp.CustomerId, followUp.LeadId, followUp.OpportunityId, followUp.AssignedTo);
        return View(model);
    }

    // POST: FollowUps/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, FollowUpInputModel model)
    {
        if (id != model.FollowUpId)
        {
            return BadRequest();
        }

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isSalesExec = User.IsInRole("SalesExecutive");

        var followUp = await _context.FollowUps.FindAsync(id);
        if (followUp == null)
        {
            return NotFound();
        }

        if (isSalesExec && followUp.AssignedTo != currentUserId)
        {
            return Forbid();
        }

        if (model.Status == "Planned" && model.FollowUpDate.Date < DateTime.Today)
        {
            ModelState.AddModelError("FollowUpDate", "Follow-up date cannot be earlier than today.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync(model.CustomerId, model.LeadId, model.OpportunityId, model.AssignedTo);
            return View(model);
        }

        var oldSummary = $"Type: {followUp.FollowUpType}, Date: {followUp.FollowUpDate:g}, Status: {followUp.Status}";

        followUp.CustomerId = model.CustomerId;
        followUp.LeadId = model.LeadId;
        followUp.OpportunityId = model.OpportunityId;
        followUp.FollowUpDate = model.FollowUpDate;
        followUp.FollowUpType = model.FollowUpType;
        followUp.Remarks = model.Remarks.Trim();
        followUp.Status = model.Status;

        if (!isSalesExec)
        {
            followUp.AssignedTo = model.AssignedTo;
        }

        _context.Update(followUp);
        await _context.SaveChangesAsync();

        var newSummary = $"Type: {followUp.FollowUpType}, Date: {followUp.FollowUpDate:g}, Status: {followUp.Status}";
        await _auditService.LogAsync("Update", "FollowUp", followUp.FollowUpId.ToString(), oldSummary, newSummary);

        TempData["SuccessMessage"] = "Follow-up updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    // POST: FollowUps/MarkComplete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkComplete(int id)
    {
        var followUp = await _context.FollowUps.FindAsync(id);
        if (followUp == null)
        {
            return NotFound();
        }

        followUp.Status = "Completed";
        _context.Update(followUp);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync("Update", "FollowUp", id.ToString(), "Status: Planned", "Status: Completed");

        TempData["SuccessMessage"] = "Follow-up marked as completed.";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateDropdownsAsync(int? customerId = null, int? leadId = null, int? opportunityId = null, string? assignedTo = null)
    {
        var customers = await _context.Customers.Where(c => c.Status == "Active").ToListAsync();
        ViewBag.CustomersList = new SelectList(customers, "CustomerId", "CustomerName", customerId);

        var leads = await _context.Leads.Where(l => l.Status != "Lost").ToListAsync();
        ViewBag.LeadsList = new SelectList(leads, "LeadId", "LeadName", leadId);

        var opps = await _context.Opportunities.Where(o => o.Status == "Open").ToListAsync();
        ViewBag.OpportunitiesList = new SelectList(opps, "OpportunityId", "OpportunityName", opportunityId);

        var users = await _userManager.Users.Where(u => u.IsActive).ToListAsync();
        ViewBag.UsersList = new SelectList(users, "Id", "FullName", assignedTo);
    }
}
