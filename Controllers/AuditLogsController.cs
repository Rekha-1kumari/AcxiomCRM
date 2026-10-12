using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;

namespace AcxiomCRM.Controllers;

[Authorize(Roles = "Admin")]
public class AuditLogsController : Controller
{
    private readonly ApplicationDbContext _context;

    public AuditLogsController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? userFilter, string? moduleFilter, string? actionFilter, DateTime? startDate, DateTime? endDate, int page = 1)
    {
        IQueryable<Models.AuditLog> query = _context.AuditLogs;

        if (!string.IsNullOrWhiteSpace(userFilter))
        {
            query = query.Where(a => (a.UserName != null && a.UserName.Contains(userFilter)) || (a.UserId != null && a.UserId.Contains(userFilter)));
        }

        if (!string.IsNullOrWhiteSpace(moduleFilter))
        {
            query = query.Where(a => a.EntityName == moduleFilter);
        }

        if (!string.IsNullOrWhiteSpace(actionFilter))
        {
            query = query.Where(a => a.Action == actionFilter);
        }

        if (startDate.HasValue)
        {
            query = query.Where(a => a.CreatedDate >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            var endOfDay = endDate.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(a => a.CreatedDate <= endOfDay);
        }

        ViewBag.UserFilter = userFilter;
        ViewBag.ModuleFilter = moduleFilter;
        ViewBag.ActionFilter = actionFilter;
        ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
        ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");

        const int pageSize = 15;
        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        page = Math.Max(1, Math.Min(page, Math.Max(1, totalPages)));

        var logs = await query
            .OrderByDescending(a => a.CreatedDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalCount = totalItems;

        return View(logs);
    }
}
