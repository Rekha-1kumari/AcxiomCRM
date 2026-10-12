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
public class OpportunitiesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditService _auditService;

    public OpportunitiesController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IAuditService auditService)
    {
        _context = context;
        _userManager = userManager;
        _auditService = auditService;
    }

    // GET: Opportunities
    public async Task<IActionResult> Index(string? search, string? stage, string? status, int page = 1)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isSalesExec = User.IsInRole("SalesExecutive");

        IQueryable<Opportunity> query = _context.Opportunities
            .Include(o => o.Customer)
            .Include(o => o.Lead);

        if (isSalesExec && !string.IsNullOrEmpty(currentUserId))
        {
            query = query.Where(o => o.AssignedTo == currentUserId);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(o =>
                o.OpportunityName.ToLower().Contains(s) ||
                (o.Customer != null && o.Customer.CustomerName.ToLower().Contains(s)) ||
                (o.Lead != null && o.Lead.LeadName.ToLower().Contains(s)));
        }

        if (!string.IsNullOrWhiteSpace(stage))
        {
            query = query.Where(o => o.Stage == stage);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(o => o.Status == status);
        }

        ViewBag.Search = search;
        ViewBag.Stage = stage;
        ViewBag.Status = status;

        const int pageSize = 10;
        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        page = Math.Max(1, Math.Min(page, Math.Max(1, totalPages)));

        var opportunities = await query
            .OrderByDescending(o => o.CreatedDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalCount = totalItems;

        var users = await _userManager.Users.ToDictionaryAsync(u => u.Id, u => u.FullName);
        ViewBag.UsersLookup = users;

        return View(opportunities);
    }

    // GET: Opportunities/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isSalesExec = User.IsInRole("SalesExecutive");

        var opportunity = await _context.Opportunities
            .Include(o => o.Customer)
            .Include(o => o.Lead)
            .Include(o => o.FollowUps)
            .FirstOrDefaultAsync(o => o.OpportunityId == id);

        if (opportunity == null)
        {
            return NotFound();
        }

        if (isSalesExec && opportunity.AssignedTo != currentUserId)
        {
            return Forbid();
        }

        string? assignedName = null;
        if (!string.IsNullOrEmpty(opportunity.AssignedTo))
        {
            var user = await _userManager.FindByIdAsync(opportunity.AssignedTo);
            assignedName = user?.FullName ?? user?.UserName;
        }

        var model = new OpportunityDetailsViewModel
        {
            Opportunity = opportunity,
            AssignedUserName = assignedName,
            FollowUps = opportunity.FollowUps.OrderByDescending(f => f.FollowUpDate).ToList()
        };

        return View(model);
    }

    // GET: Opportunities/Create
    public async Task<IActionResult> Create(int? customerId = null, int? leadId = null)
    {
        await PopulateDropdownsAsync(customerId, leadId);
        return View(new OpportunityInputModel
        {
            CustomerId = customerId,
            LeadId = leadId,
            ExpectedCloseDate = DateTime.Today.AddDays(14)
        });
    }

    // POST: Opportunities/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(OpportunityInputModel model)
    {
        // Mandatory Business Validations
        if (model.Amount <= 0)
        {
            ModelState.AddModelError("Amount", "Opportunity Amount must be greater than 0.");
        }

        if (model.Probability < 0 || model.Probability > 100)
        {
            ModelState.AddModelError("Probability", "Probability must be between 0 and 100.");
        }

        // Expected Close Date cannot be in the past for an active opportunity
        if (model.Status == "Open" && model.ExpectedCloseDate.Date < DateTime.Today)
        {
            ModelState.AddModelError("ExpectedCloseDate", "Expected Close Date cannot be in the past.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync(model.CustomerId, model.LeadId, model.AssignedTo);
            return View(model);
        }

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // Auto-synchronize Stage & Status if Won/Lost selected
        var status = model.Status;
        if (model.Stage == "Won") status = "Won";
        else if (model.Stage == "Lost") status = "Lost";

        var opportunity = new Opportunity
        {
            OpportunityName = model.OpportunityName.Trim(),
            CustomerId = model.CustomerId,
            LeadId = model.LeadId,
            Amount = model.Amount,
            Stage = model.Stage,
            Probability = model.Probability,
            ExpectedCloseDate = model.ExpectedCloseDate,
            Status = status,
            CreatedDate = DateTime.UtcNow,
            AssignedTo = User.IsInRole("SalesExecutive") ? currentUserId : model.AssignedTo,
            Notes = model.Notes?.Trim()
        };

        _context.Opportunities.Add(opportunity);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync("Create", "Opportunity", opportunity.OpportunityId.ToString(), null,
            $"Created Opportunity: {opportunity.OpportunityName} - Amount: {opportunity.Amount}, Stage: {opportunity.Stage}");

        TempData["SuccessMessage"] = $"Opportunity '{opportunity.OpportunityName}' created successfully.";
        return RedirectToAction(nameof(Index));
    }

    // GET: Opportunities/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isSalesExec = User.IsInRole("SalesExecutive");

        var opportunity = await _context.Opportunities.FindAsync(id);
        if (opportunity == null)
        {
            return NotFound();
        }

        if (isSalesExec && opportunity.AssignedTo != currentUserId)
        {
            return Forbid();
        }

        var model = new OpportunityInputModel
        {
            OpportunityId = opportunity.OpportunityId,
            OpportunityName = opportunity.OpportunityName,
            CustomerId = opportunity.CustomerId,
            LeadId = opportunity.LeadId,
            Amount = opportunity.Amount,
            Stage = opportunity.Stage,
            Probability = opportunity.Probability,
            ExpectedCloseDate = opportunity.ExpectedCloseDate,
            Status = opportunity.Status,
            AssignedTo = opportunity.AssignedTo,
            Notes = opportunity.Notes
        };

        await PopulateDropdownsAsync(opportunity.CustomerId, opportunity.LeadId, opportunity.AssignedTo);
        return View(model);
    }

    // POST: Opportunities/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, OpportunityInputModel model)
    {
        if (id != model.OpportunityId)
        {
            return BadRequest();
        }

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isSalesExec = User.IsInRole("SalesExecutive");

        var opportunity = await _context.Opportunities.FindAsync(id);
        if (opportunity == null)
        {
            return NotFound();
        }

        if (isSalesExec && opportunity.AssignedTo != currentUserId)
        {
            return Forbid();
        }

        // Mandatory Business Validations
        if (model.Amount <= 0)
        {
            ModelState.AddModelError("Amount", "Opportunity Amount must be greater than 0.");
        }

        if (model.Probability < 0 || model.Probability > 100)
        {
            ModelState.AddModelError("Probability", "Probability must be between 0 and 100.");
        }

        if (model.Status == "Open" && model.ExpectedCloseDate.Date < DateTime.Today)
        {
            ModelState.AddModelError("ExpectedCloseDate", "Expected Close Date cannot be in the past.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync(model.CustomerId, model.LeadId, model.AssignedTo);
            return View(model);
        }

        var oldSummary = $"Name: {opportunity.OpportunityName}, Stage: {opportunity.Stage}, Amount: {opportunity.Amount}";

        var status = model.Status;
        if (model.Stage == "Won") status = "Won";
        else if (model.Stage == "Lost") status = "Lost";

        opportunity.OpportunityName = model.OpportunityName.Trim();
        opportunity.CustomerId = model.CustomerId;
        opportunity.LeadId = model.LeadId;
        opportunity.Amount = model.Amount;
        opportunity.Stage = model.Stage;
        opportunity.Probability = model.Probability;
        opportunity.ExpectedCloseDate = model.ExpectedCloseDate;
        opportunity.Status = status;
        opportunity.Notes = model.Notes?.Trim();

        if (!isSalesExec)
        {
            opportunity.AssignedTo = model.AssignedTo;
        }

        _context.Update(opportunity);
        await _context.SaveChangesAsync();

        var newSummary = $"Name: {opportunity.OpportunityName}, Stage: {opportunity.Stage}, Amount: {opportunity.Amount}";
        await _auditService.LogAsync("Update", "Opportunity", opportunity.OpportunityId.ToString(), oldSummary, newSummary);

        TempData["SuccessMessage"] = $"Opportunity '{opportunity.OpportunityName}' updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    // POST: Opportunities/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Delete(int id)
    {
        var opportunity = await _context.Opportunities.FindAsync(id);
        if (opportunity == null)
        {
            return NotFound();
        }

        var name = opportunity.OpportunityName;
        opportunity.Status = "Lost";
        _context.Update(opportunity);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync("Delete", "Opportunity", id.ToString(), $"Opportunity {name}", $"Opportunity {name} marked Lost");

        TempData["SuccessMessage"] = $"Opportunity '{name}' marked as Lost.";
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
