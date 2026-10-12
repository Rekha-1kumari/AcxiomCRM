using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.ViewModels;

namespace AcxiomCRM.Controllers;

[Authorize]
public class ReportsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public ReportsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(string reportType = "Pipeline", DateTime? startDate = null, DateTime? endDate = null, string? status = null)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isSalesExec = User.IsInRole("SalesExecutive");
        var usersDict = await _userManager.Users.ToDictionaryAsync(u => u.Id, u => u.FullName);

        var model = new ReportFilterViewModel
        {
            ReportType = reportType,
            StartDate = startDate,
            EndDate = endDate,
            Status = status
        };

        if (reportType == "Pipeline")
        {
            IQueryable<Opportunity> query = _context.Opportunities;
            if (isSalesExec && !string.IsNullOrEmpty(currentUserId)) query = query.Where(o => o.AssignedTo == currentUserId);
            if (startDate.HasValue) query = query.Where(o => o.CreatedDate >= startDate.Value);
            if (endDate.HasValue) query = query.Where(o => o.CreatedDate <= endDate.Value.Date.AddDays(1).AddTicks(-1));

            var opps = await query.ToListAsync();
            var stages = new[] { "Qualification", "Proposal", "Negotiation", "Won", "Lost" };

            foreach (var stage in stages)
            {
                var stageOpps = opps.Where(o => o.Stage == stage).ToList();
                model.PipelineSummaries.Add(new PipelineStageSummary
                {
                    Stage = stage,
                    Count = stageOpps.Count,
                    TotalAmount = stageOpps.Sum(o => o.Amount),
                    WeightedAmount = stageOpps.Sum(o => o.WeightedAmount)
                });
            }
        }
        else if (reportType == "Customer")
        {
            IQueryable<Customer> query = _context.Customers;
            if (isSalesExec && !string.IsNullOrEmpty(currentUserId)) query = query.Where(c => c.AssignedTo == currentUserId);
            if (!string.IsNullOrEmpty(status)) query = query.Where(c => c.Status == status);
            if (startDate.HasValue) query = query.Where(c => c.CreatedDate >= startDate.Value);
            if (endDate.HasValue) query = query.Where(c => c.CreatedDate <= endDate.Value.Date.AddDays(1).AddTicks(-1));

            var list = await query.OrderByDescending(c => c.CreatedDate).ToListAsync();
            model.CustomerReports = list.Select(c => new CustomerReportItem
            {
                CustomerCode = c.CustomerCode,
                CustomerName = c.CustomerName,
                Email = c.Email,
                Phone = c.Phone,
                CompanyName = c.CompanyName,
                Status = c.Status,
                OwnerName = c.AssignedTo != null && usersDict.ContainsKey(c.AssignedTo) ? usersDict[c.AssignedTo] : "Unassigned",
                CreatedDate = c.CreatedDate
            }).ToList();
        }
        else if (reportType == "Lead")
        {
            IQueryable<Lead> query = _context.Leads;
            if (isSalesExec && !string.IsNullOrEmpty(currentUserId)) query = query.Where(l => l.AssignedTo == currentUserId);
            if (!string.IsNullOrEmpty(status)) query = query.Where(l => l.Status == status);
            if (startDate.HasValue) query = query.Where(l => l.CreatedDate >= startDate.Value);
            if (endDate.HasValue) query = query.Where(l => l.CreatedDate <= endDate.Value.Date.AddDays(1).AddTicks(-1));

            var list = await query.OrderByDescending(l => l.CreatedDate).ToListAsync();
            model.LeadReports = list.Select(l => new LeadReportItem
            {
                LeadCode = l.LeadCode,
                LeadName = l.LeadName,
                CompanyName = l.CompanyName,
                Source = l.Source,
                Status = l.Status,
                ExpectedValue = l.ExpectedValue,
                OwnerName = l.AssignedTo != null && usersDict.ContainsKey(l.AssignedTo) ? usersDict[l.AssignedTo] : "Unassigned",
                CreatedDate = l.CreatedDate,
                IsConverted = l.Status == "Converted"
            }).ToList();
        }
        else if (reportType == "Opportunity")
        {
            IQueryable<Opportunity> query = _context.Opportunities.Include(o => o.Customer);
            if (isSalesExec && !string.IsNullOrEmpty(currentUserId)) query = query.Where(o => o.AssignedTo == currentUserId);
            if (!string.IsNullOrEmpty(status)) query = query.Where(o => o.Status == status);
            if (startDate.HasValue) query = query.Where(o => o.CreatedDate >= startDate.Value);
            if (endDate.HasValue) query = query.Where(o => o.CreatedDate <= endDate.Value.Date.AddDays(1).AddTicks(-1));

            var list = await query.OrderByDescending(o => o.CreatedDate).ToListAsync();
            model.OpportunityReports = list.Select(o => new OpportunityReportItem
            {
                OpportunityName = o.OpportunityName,
                CustomerName = o.Customer?.CustomerName ?? "N/A",
                Amount = o.Amount,
                Stage = o.Stage,
                Probability = o.Probability,
                WeightedAmount = o.WeightedAmount,
                ExpectedCloseDate = o.ExpectedCloseDate,
                Status = o.Status,
                OwnerName = o.AssignedTo != null && usersDict.ContainsKey(o.AssignedTo) ? usersDict[o.AssignedTo] : "Unassigned"
            }).ToList();
        }
        else if (reportType == "FollowUp")
        {
            IQueryable<FollowUp> query = _context.FollowUps.Include(f => f.Customer).Include(f => f.Lead);
            if (isSalesExec && !string.IsNullOrEmpty(currentUserId)) query = query.Where(f => f.AssignedTo == currentUserId);
            if (!string.IsNullOrEmpty(status)) query = query.Where(f => f.Status == status);

            var list = await query.OrderByDescending(f => f.FollowUpDate).ToListAsync();
            model.FollowUpReports = list.Select(f => new FollowUpReportItem
            {
                FollowUpId = f.FollowUpId,
                FollowUpDate = f.FollowUpDate,
                FollowUpType = f.FollowUpType,
                Remarks = f.Remarks,
                Status = f.Status,
                RelatedTo = f.Customer?.CustomerName ?? f.Lead?.LeadName ?? "N/A",
                AssignedUserName = f.AssignedTo != null && usersDict.ContainsKey(f.AssignedTo) ? usersDict[f.AssignedTo] : "Unassigned",
                IsOverdue = f.Status == "Planned" && f.FollowUpDate < DateTime.Now
            }).ToList();
        }
        else if (reportType == "Conversion")
        {
            IQueryable<Lead> query = _context.Leads;
            if (isSalesExec && !string.IsNullOrEmpty(currentUserId)) query = query.Where(l => l.AssignedTo == currentUserId);

            var total = await query.CountAsync();
            var converted = await query.CountAsync(l => l.Status == "Converted");
            var lost = await query.CountAsync(l => l.Status == "Lost");
            var open = total - converted - lost;

            model.ConversionSummary = new ConversionSummary
            {
                TotalLeads = total,
                ConvertedLeads = converted,
                LostLeads = lost,
                OpenLeads = Math.Max(0, open)
            };
        }
        else if (reportType == "UserActivity" && (User.IsInRole("Admin") || User.IsInRole("Manager")))
        {
            var users = await _userManager.Users.ToListAsync();
            foreach (var u in users)
            {
                var roles = await _userManager.GetRolesAsync(u);
                model.UserActivities.Add(new UserActivitySummary
                {
                    UserName = u.FullName,
                    Role = roles.FirstOrDefault() ?? "None",
                    CustomersCount = await _context.Customers.CountAsync(c => c.AssignedTo == u.Id),
                    LeadsCount = await _context.Leads.CountAsync(l => l.AssignedTo == u.Id),
                    OpportunitiesCount = await _context.Opportunities.CountAsync(o => o.AssignedTo == u.Id),
                    ActivitiesCount = await _context.Activities.CountAsync(a => a.AssignedTo == u.Id),
                    FollowUpsCount = await _context.FollowUps.CountAsync(f => f.AssignedTo == u.Id)
                });
            }
        }
        else if (reportType == "Audit" && User.IsInRole("Admin"))
        {
            model.AuditLogs = await _context.AuditLogs
                .OrderByDescending(a => a.CreatedDate)
                .Take(50)
                .ToListAsync();
        }

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> ExportCsv(string reportType = "Pipeline")
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isSalesExec = User.IsInRole("SalesExecutive");
        var csv = new StringBuilder();

        if (reportType == "Customer")
        {
            IQueryable<Customer> query = _context.Customers;
            if (isSalesExec && !string.IsNullOrEmpty(currentUserId)) query = query.Where(c => c.AssignedTo == currentUserId);

            var list = await query.ToListAsync();
            csv.AppendLine("CustomerCode,CustomerName,Email,Phone,Company,Status,CreatedDate");
            foreach (var c in list)
            {
                csv.AppendLine($"\"{c.CustomerCode}\",\"{c.CustomerName}\",\"{c.Email}\",\"{c.Phone}\",\"{c.CompanyName}\",\"{c.Status}\",\"{c.CreatedDate:yyyy-MM-dd}\"");
            }
        }
        else if (reportType == "Lead")
        {
            IQueryable<Lead> query = _context.Leads;
            if (isSalesExec && !string.IsNullOrEmpty(currentUserId)) query = query.Where(l => l.AssignedTo == currentUserId);

            var list = await query.ToListAsync();
            csv.AppendLine("LeadCode,LeadName,Email,Phone,Company,Source,Status,ExpectedValue,CreatedDate");
            foreach (var l in list)
            {
                csv.AppendLine($"\"{l.LeadCode}\",\"{l.LeadName}\",\"{l.Email}\",\"{l.Phone}\",\"{l.CompanyName}\",\"{l.Source}\",\"{l.Status}\",{l.ExpectedValue},\"{l.CreatedDate:yyyy-MM-dd}\"");
            }
        }
        else
        {
            IQueryable<Opportunity> query = _context.Opportunities.Include(o => o.Customer);
            if (isSalesExec && !string.IsNullOrEmpty(currentUserId)) query = query.Where(o => o.AssignedTo == currentUserId);

            var list = await query.ToListAsync();
            csv.AppendLine("OpportunityName,Customer,Amount,Stage,Probability,WeightedAmount,Status,ExpectedCloseDate");
            foreach (var o in list)
            {
                csv.AppendLine($"\"{o.OpportunityName}\",\"{o.Customer?.CustomerName}\",{o.Amount},\"{o.Stage}\",{o.Probability},{o.WeightedAmount},\"{o.Status}\",\"{o.ExpectedCloseDate:yyyy-MM-dd}\"");
            }
        }

        var bytes = Encoding.UTF8.GetBytes(csv.ToString());
        return File(bytes, "text/csv", $"{reportType}_Report_{DateTime.UtcNow:yyyyMMdd}.csv");
    }
}
