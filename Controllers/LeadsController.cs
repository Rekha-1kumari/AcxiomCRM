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
public class LeadsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditService _auditService;

    public LeadsController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IAuditService auditService)
    {
        _context = context;
        _userManager = userManager;
        _auditService = auditService;
    }

    // GET: Leads
    public async Task<IActionResult> Index(string? search, string? status, string? source, int page = 1)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isSalesExec = User.IsInRole("SalesExecutive");

        IQueryable<Lead> query = _context.Leads;

        if (isSalesExec && !string.IsNullOrEmpty(currentUserId))
        {
            query = query.Where(l => l.AssignedTo == currentUserId);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(l =>
                l.LeadName.ToLower().Contains(s) ||
                l.Email.ToLower().Contains(s) ||
                l.Phone.Contains(s) ||
                (l.CompanyName != null && l.CompanyName.ToLower().Contains(s)));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(l => l.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(source))
        {
            query = query.Where(l => l.Source == source);
        }

        ViewBag.Search = search;
        ViewBag.Status = status;
        ViewBag.Source = source;

        const int pageSize = 10;
        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        page = Math.Max(1, Math.Min(page, Math.Max(1, totalPages)));

        var leads = await query
            .OrderByDescending(l => l.CreatedDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalCount = totalItems;

        var users = await _userManager.Users.ToDictionaryAsync(u => u.Id, u => u.FullName);
        ViewBag.UsersLookup = users;

        return View(leads);
    }

    // GET: Leads/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isSalesExec = User.IsInRole("SalesExecutive");

        var lead = await _context.Leads
            .Include(l => l.FollowUps)
            .Include(l => l.Activities)
            .FirstOrDefaultAsync(l => l.LeadId == id);

        if (lead == null)
        {
            return NotFound();
        }

        if (isSalesExec && lead.AssignedTo != currentUserId)
        {
            return Forbid();
        }

        string? assignedName = null;
        if (!string.IsNullOrEmpty(lead.AssignedTo))
        {
            var user = await _userManager.FindByIdAsync(lead.AssignedTo);
            assignedName = user?.FullName ?? user?.UserName;
        }

        Customer? convertedCust = null;
        if (lead.ConvertedCustomerId.HasValue)
        {
            convertedCust = await _context.Customers.FindAsync(lead.ConvertedCustomerId.Value);
        }

        Opportunity? convertedOpp = null;
        if (lead.ConvertedOpportunityId.HasValue)
        {
            convertedOpp = await _context.Opportunities.FindAsync(lead.ConvertedOpportunityId.Value);
        }

        var model = new LeadDetailsViewModel
        {
            Lead = lead,
            AssignedUserName = assignedName,
            ConvertedCustomer = convertedCust,
            ConvertedOpportunity = convertedOpp,
            FollowUps = lead.FollowUps.OrderByDescending(f => f.FollowUpDate).ToList(),
            Activities = lead.Activities.OrderByDescending(a => a.ActivityDate).ToList()
        };

        return View(model);
    }

    // GET: Leads/Create
    public async Task<IActionResult> Create()
    {
        await PopulateUsersDropdownAsync();
        var nextCode = "LEAD-" + (2000 + await _context.Leads.CountAsync() + 1);
        return View(new LeadInputModel { LeadCode = nextCode });
    }

    // POST: Leads/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(LeadInputModel model)
    {
        // Status validity check
        var validStatuses = new[] { "New", "Contacted", "Qualified", "Unqualified", "Converted", "Lost" };
        if (!validStatuses.Contains(model.Status))
        {
            ModelState.AddModelError("Status", "Invalid lead status selected.");
        }

        if (model.ExpectedValue < 0)
        {
            ModelState.AddModelError("ExpectedValue", "Expected Value cannot be negative.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateUsersDropdownAsync();
            return View(model);
        }

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var code = model.LeadCode;
        if (string.IsNullOrWhiteSpace(code) || await _context.Leads.AnyAsync(l => l.LeadCode == code))
        {
            code = "LEAD-" + (2000 + await _context.Leads.CountAsync() + 1);
        }

        var lead = new Lead
        {
            LeadCode = code,
            LeadName = model.LeadName.Trim(),
            Email = model.Email.Trim().ToLower(),
            Phone = model.Phone.Trim(),
            CompanyName = model.CompanyName?.Trim(),
            Source = model.Source,
            Status = model.Status,
            ExpectedValue = model.ExpectedValue,
            CreatedDate = DateTime.UtcNow,
            AssignedTo = User.IsInRole("SalesExecutive") ? currentUserId : model.AssignedTo
        };

        _context.Leads.Add(lead);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync("Create", "Lead", lead.LeadId.ToString(), null,
            $"Created Lead: {lead.LeadName} ({lead.LeadCode}) - {lead.Status}");

        TempData["SuccessMessage"] = $"Lead '{lead.LeadName}' created successfully.";
        return RedirectToAction(nameof(Index));
    }

    // GET: Leads/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isSalesExec = User.IsInRole("SalesExecutive");

        var lead = await _context.Leads.FindAsync(id);
        if (lead == null)
        {
            return NotFound();
        }

        if (isSalesExec && lead.AssignedTo != currentUserId)
        {
            return Forbid();
        }

        var model = new LeadInputModel
        {
            LeadId = lead.LeadId,
            LeadCode = lead.LeadCode,
            LeadName = lead.LeadName,
            Email = lead.Email,
            Phone = lead.Phone,
            CompanyName = lead.CompanyName,
            Source = lead.Source,
            Status = lead.Status,
            ExpectedValue = lead.ExpectedValue,
            AssignedTo = lead.AssignedTo
        };

        await PopulateUsersDropdownAsync(lead.AssignedTo);
        return View(model);
    }

    // POST: Leads/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, LeadInputModel model)
    {
        if (id != model.LeadId)
        {
            return BadRequest();
        }

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isSalesExec = User.IsInRole("SalesExecutive");

        var lead = await _context.Leads.FindAsync(id);
        if (lead == null)
        {
            return NotFound();
        }

        if (isSalesExec && lead.AssignedTo != currentUserId)
        {
            return Forbid();
        }

        var validStatuses = new[] { "New", "Contacted", "Qualified", "Unqualified", "Converted", "Lost" };
        if (!validStatuses.Contains(model.Status))
        {
            ModelState.AddModelError("Status", "Invalid lead status selected.");
        }

        if (model.ExpectedValue < 0)
        {
            ModelState.AddModelError("ExpectedValue", "Expected Value cannot be negative.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateUsersDropdownAsync(model.AssignedTo);
            return View(model);
        }

        var oldSummary = $"Name: {lead.LeadName}, Status: {lead.Status}, Value: {lead.ExpectedValue}";

        lead.LeadName = model.LeadName.Trim();
        lead.Email = model.Email.Trim().ToLower();
        lead.Phone = model.Phone.Trim();
        lead.CompanyName = model.CompanyName?.Trim();
        lead.Source = model.Source;
        lead.Status = model.Status;
        lead.ExpectedValue = model.ExpectedValue;

        if (!isSalesExec)
        {
            lead.AssignedTo = model.AssignedTo;
        }

        _context.Update(lead);
        await _context.SaveChangesAsync();

        var newSummary = $"Name: {lead.LeadName}, Status: {lead.Status}, Value: {lead.ExpectedValue}";
        await _auditService.LogAsync("Update", "Lead", lead.LeadId.ToString(), oldSummary, newSummary);

        TempData["SuccessMessage"] = $"Lead '{lead.LeadName}' updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    // GET: Leads/Convert/5
    public async Task<IActionResult> Convert(int id)
    {
        var lead = await _context.Leads.FindAsync(id);
        if (lead == null)
        {
            return NotFound();
        }

        if (lead.Status == "Converted")
        {
            TempData["ErrorMessage"] = "This lead is already converted.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var model = new LeadConvertViewModel
        {
            LeadId = lead.LeadId,
            LeadName = lead.LeadName,
            Email = lead.Email,
            Phone = lead.Phone,
            CompanyName = lead.CompanyName,
            OpportunityName = $"{lead.CompanyName ?? lead.LeadName} - Deal",
            OpportunityAmount = lead.ExpectedValue > 0 ? lead.ExpectedValue : 100000,
            OpportunityStage = "Proposal",
            Probability = 50,
            ExpectedCloseDate = DateTime.Today.AddDays(30)
        };

        return View(model);
    }

    // POST: Leads/Convert/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Convert(int id, LeadConvertViewModel model)
    {
        if (id != model.LeadId)
        {
            return BadRequest();
        }

        var lead = await _context.Leads.FindAsync(id);
        if (lead == null)
        {
            return NotFound();
        }

        if (model.CreateOpportunity)
        {
            if (model.OpportunityAmount <= 0)
            {
                ModelState.AddModelError("OpportunityAmount", "Opportunity Amount must be greater than 0.");
            }
            if (model.Probability < 0 || model.Probability > 100)
            {
                ModelState.AddModelError("Probability", "Probability must be between 0 and 100.");
            }
            if (model.ExpectedCloseDate.Date < DateTime.Today)
            {
                ModelState.AddModelError("ExpectedCloseDate", "Expected Close Date cannot be in the past.");
            }
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        Customer? newCustomer = null;

        // 1. Create Customer
        if (model.CreateCustomer)
        {
            var existingCustomer = await _context.Customers.FirstOrDefaultAsync(c => c.Email == lead.Email || c.Phone == lead.Phone);
            if (existingCustomer != null)
            {
                newCustomer = existingCustomer;
            }
            else
            {
                newCustomer = new Customer
                {
                    CustomerCode = "CUST-" + (1000 + await _context.Customers.CountAsync() + 1),
                    CustomerName = lead.LeadName,
                    CompanyName = lead.CompanyName,
                    Email = lead.Email,
                    Phone = lead.Phone,
                    Status = "Active",
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = currentUserId,
                    AssignedTo = lead.AssignedTo ?? currentUserId
                };
                _context.Customers.Add(newCustomer);
                await _context.SaveChangesAsync();
            }
        }

        // 2. Create Opportunity
        Opportunity? newOpportunity = null;
        if (model.CreateOpportunity)
        {
            newOpportunity = new Opportunity
            {
                OpportunityName = string.IsNullOrWhiteSpace(model.OpportunityName) ? $"{lead.LeadName} Deal" : model.OpportunityName.Trim(),
                CustomerId = newCustomer?.CustomerId,
                LeadId = lead.LeadId,
                Amount = model.OpportunityAmount,
                Stage = model.OpportunityStage,
                Probability = model.Probability,
                ExpectedCloseDate = model.ExpectedCloseDate,
                Status = "Open",
                CreatedDate = DateTime.UtcNow,
                AssignedTo = lead.AssignedTo ?? currentUserId
            };
            _context.Opportunities.Add(newOpportunity);
            await _context.SaveChangesAsync();
        }

        // 3. Update Lead Status to Converted
        lead.Status = "Converted";
        lead.ConvertedCustomerId = newCustomer?.CustomerId;
        lead.ConvertedOpportunityId = newOpportunity?.OpportunityId;
        _context.Update(lead);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync("Convert", "Lead", lead.LeadId.ToString(), null,
            $"Lead {lead.LeadName} converted. Customer ID: {newCustomer?.CustomerId}, Opportunity ID: {newOpportunity?.OpportunityId}");

        TempData["SuccessMessage"] = $"Lead '{lead.LeadName}' successfully converted!";
        return RedirectToAction(nameof(Details), new { id = lead.LeadId });
    }

    // POST: Leads/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Delete(int id)
    {
        var lead = await _context.Leads.FindAsync(id);
        if (lead == null)
        {
            return NotFound();
        }

        var name = lead.LeadName;
        lead.Status = "Lost";
        _context.Update(lead);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync("Delete", "Lead", id.ToString(), $"Lead {name}", $"Lead {name} marked Lost");

        TempData["SuccessMessage"] = $"Lead '{name}' marked as Lost.";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateUsersDropdownAsync(string? selectedUserId = null)
    {
        var users = await _userManager.Users.Where(u => u.IsActive).ToListAsync();
        ViewBag.UsersList = new SelectList(users, "Id", "FullName", selectedUserId);
    }
}
