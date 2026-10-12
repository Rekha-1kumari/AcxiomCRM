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
public class CustomersController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditService _auditService;

    public CustomersController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IAuditService auditService)
    {
        _context = context;
        _userManager = userManager;
        _auditService = auditService;
    }

    // GET: Customers
    public async Task<IActionResult> Index(string? search, string? status, int page = 1)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isSalesExec = User.IsInRole("SalesExecutive");

        IQueryable<Customer> query = _context.Customers;

        if (isSalesExec && !string.IsNullOrEmpty(currentUserId))
        {
            query = query.Where(c => c.AssignedTo == currentUserId);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(c =>
                c.CustomerName.ToLower().Contains(s) ||
                c.Email.ToLower().Contains(s) ||
                c.Phone.Contains(s) ||
                (c.CompanyName != null && c.CompanyName.ToLower().Contains(s)));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(c => c.Status == status);
        }

        ViewBag.Search = search;
        ViewBag.Status = status;

        const int pageSize = 10;
        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        page = Math.Max(1, Math.Min(page, Math.Max(1, totalPages)));

        var customers = await query
            .OrderByDescending(c => c.CreatedDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalCount = totalItems;

        // Fetch user lookup for display
        var users = await _userManager.Users.ToDictionaryAsync(u => u.Id, u => u.FullName);
        ViewBag.UsersLookup = users;

        return View(customers);
    }

    // GET: Customers/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isSalesExec = User.IsInRole("SalesExecutive");

        var customer = await _context.Customers
            .Include(c => c.Opportunities)
            .Include(c => c.FollowUps)
            .Include(c => c.Activities)
            .FirstOrDefaultAsync(m => m.CustomerId == id);

        if (customer == null)
        {
            return NotFound();
        }

        if (isSalesExec && customer.AssignedTo != currentUserId)
        {
            return Forbid();
        }

        string? assignedName = null;
        if (!string.IsNullOrEmpty(customer.AssignedTo))
        {
            var user = await _userManager.FindByIdAsync(customer.AssignedTo);
            assignedName = user?.FullName ?? user?.UserName;
        }

        var model = new CustomerDetailsViewModel
        {
            Customer = customer,
            AssignedUserName = assignedName,
            Opportunities = customer.Opportunities.OrderByDescending(o => o.CreatedDate).ToList(),
            FollowUps = customer.FollowUps.OrderByDescending(f => f.FollowUpDate).ToList(),
            Activities = customer.Activities.OrderByDescending(a => a.ActivityDate).ToList()
        };

        return View(model);
    }

    // GET: Customers/Create
    public async Task<IActionResult> Create()
    {
        await PopulateUsersDropdownAsync();
        var nextCode = "CUST-" + (1000 + await _context.Customers.CountAsync() + 1);
        return View(new CustomerInputModel { CustomerCode = nextCode });
    }

    // POST: Customers/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CustomerInputModel model)
    {
        // Server-Side Uniqueness & Duplicate Customer Validation
        if (await _context.Customers.AnyAsync(c => c.Email.ToLower() == model.Email.ToLower()))
        {
            ModelState.AddModelError("Email", "A customer with this email address already exists.");
        }

        if (await _context.Customers.AnyAsync(c => c.Phone == model.Phone))
        {
            ModelState.AddModelError("Phone", "A customer with this phone number already exists.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateUsersDropdownAsync();
            return View(model);
        }

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var code = model.CustomerCode;
        if (string.IsNullOrWhiteSpace(code) || await _context.Customers.AnyAsync(c => c.CustomerCode == code))
        {
            code = "CUST-" + (1000 + await _context.Customers.CountAsync() + 1);
        }

        var customer = new Customer
        {
            CustomerCode = code,
            CustomerName = model.CustomerName.Trim(),
            Email = model.Email.Trim().ToLower(),
            Phone = model.Phone.Trim(),
            CompanyName = model.CompanyName?.Trim(),
            Address = model.Address?.Trim(),
            City = model.City?.Trim(),
            State = model.State?.Trim(),
            Status = model.Status,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = currentUserId,
            AssignedTo = User.IsInRole("SalesExecutive") ? currentUserId : model.AssignedTo
        };

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync("Create", "Customer", customer.CustomerId.ToString(), null,
            $"Created Customer: {customer.CustomerName} ({customer.CustomerCode}) - {customer.Email}");

        TempData["SuccessMessage"] = $"Customer '{customer.CustomerName}' created successfully.";
        return RedirectToAction(nameof(Index));
    }

    // GET: Customers/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isSalesExec = User.IsInRole("SalesExecutive");

        var customer = await _context.Customers.FindAsync(id);
        if (customer == null)
        {
            return NotFound();
        }

        if (isSalesExec && customer.AssignedTo != currentUserId)
        {
            return Forbid();
        }

        var model = new CustomerInputModel
        {
            CustomerId = customer.CustomerId,
            CustomerCode = customer.CustomerCode,
            CustomerName = customer.CustomerName,
            Email = customer.Email,
            Phone = customer.Phone,
            CompanyName = customer.CompanyName,
            Address = customer.Address,
            City = customer.City,
            State = customer.State,
            Status = customer.Status,
            AssignedTo = customer.AssignedTo
        };

        await PopulateUsersDropdownAsync(customer.AssignedTo);
        return View(model);
    }

    // POST: Customers/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CustomerInputModel model)
    {
        if (id != model.CustomerId)
        {
            return BadRequest();
        }

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isSalesExec = User.IsInRole("SalesExecutive");

        var customer = await _context.Customers.FindAsync(id);
        if (customer == null)
        {
            return NotFound();
        }

        if (isSalesExec && customer.AssignedTo != currentUserId)
        {
            return Forbid();
        }

        // Server-Side Uniqueness checks excluding current record
        if (await _context.Customers.AnyAsync(c => c.CustomerId != id && c.Email.ToLower() == model.Email.ToLower()))
        {
            ModelState.AddModelError("Email", "Another customer with this email address already exists.");
        }

        if (await _context.Customers.AnyAsync(c => c.CustomerId != id && c.Phone == model.Phone))
        {
            ModelState.AddModelError("Phone", "Another customer with this phone number already exists.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateUsersDropdownAsync(model.AssignedTo);
            return View(model);
        }

        var oldSummary = $"Name: {customer.CustomerName}, Email: {customer.Email}, Status: {customer.Status}";

        customer.CustomerName = model.CustomerName.Trim();
        customer.Email = model.Email.Trim().ToLower();
        customer.Phone = model.Phone.Trim();
        customer.CompanyName = model.CompanyName?.Trim();
        customer.Address = model.Address?.Trim();
        customer.City = model.City?.Trim();
        customer.State = model.State?.Trim();
        customer.Status = model.Status;

        if (!isSalesExec)
        {
            customer.AssignedTo = model.AssignedTo;
        }

        _context.Update(customer);
        await _context.SaveChangesAsync();

        var newSummary = $"Name: {customer.CustomerName}, Email: {customer.Email}, Status: {customer.Status}";
        await _auditService.LogAsync("Update", "Customer", customer.CustomerId.ToString(), oldSummary, newSummary);

        TempData["SuccessMessage"] = $"Customer '{customer.CustomerName}' updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    // POST: Customers/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Delete(int id)
    {
        var customer = await _context.Customers.FindAsync(id);
        if (customer == null)
        {
            return NotFound();
        }

        var name = customer.CustomerName;
        // Soft delete / Deactivate to preserve historical integrity, or removal if no related records
        customer.Status = "Inactive";
        _context.Update(customer);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync("Delete", "Customer", id.ToString(), $"Customer {name} Active", $"Customer {name} marked Inactive");

        TempData["SuccessMessage"] = $"Customer '{name}' has been deactivated.";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateUsersDropdownAsync(string? selectedUserId = null)
    {
        var users = await _userManager.Users.Where(u => u.IsActive).ToListAsync();
        ViewBag.UsersList = new SelectList(users, "Id", "FullName", selectedUserId);
    }
}
