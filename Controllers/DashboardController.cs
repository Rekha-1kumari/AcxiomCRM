using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.ViewModels;

namespace AcxiomCRM.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public DashboardController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(string dateFilter = "All")
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var currentUser = await _userManager.GetUserAsync(User);
        var isSalesExec = User.IsInRole("SalesExecutive");
        var isManager = User.IsInRole("Manager");
        var isAdmin = User.IsInRole("Admin");

        var userRole = isAdmin ? "Admin" : isManager ? "Manager" : "Sales Executive";
        var userFullName = currentUser?.FullName ?? User.Identity?.Name ?? "User";

        // Date range calculation
        DateTime? startDate = null;
        var now = DateTime.UtcNow;
        if (dateFilter == "Today")
        {
            startDate = DateTime.UtcNow.Date;
        }
        else if (dateFilter == "ThisWeek")
        {
            var dayOfWeek = (int)now.DayOfWeek;
            startDate = now.Date.AddDays(-dayOfWeek);
        }
        else if (dateFilter == "ThisMonth")
        {
            startDate = new DateTime(now.Year, now.Month, 1);
        }

        // Query foundations with Role-Based Scoping
        IQueryable<Customer> customerQuery = _context.Customers;
        IQueryable<Lead> leadQuery = _context.Leads;
        IQueryable<Opportunity> oppQuery = _context.Opportunities.Include(o => o.Customer);
        IQueryable<FollowUp> followUpQuery = _context.FollowUps.Include(f => f.Customer).Include(f => f.Lead);
        IQueryable<Activity> activityQuery = _context.Activities.Include(a => a.Customer).Include(a => a.Lead);

        // SalesExecutive only sees assigned items
        if (isSalesExec && !string.IsNullOrEmpty(currentUserId))
        {
            customerQuery = customerQuery.Where(c => c.AssignedTo == currentUserId);
            leadQuery = leadQuery.Where(l => l.AssignedTo == currentUserId);
            oppQuery = oppQuery.Where(o => o.AssignedTo == currentUserId);
            followUpQuery = followUpQuery.Where(f => f.AssignedTo == currentUserId);
            activityQuery = activityQuery.Where(a => a.AssignedTo == currentUserId);
        }

        // Date filter application
        if (startDate.HasValue)
        {
            customerQuery = customerQuery.Where(c => c.CreatedDate >= startDate.Value);
            leadQuery = leadQuery.Where(l => l.CreatedDate >= startDate.Value);
            oppQuery = oppQuery.Where(o => o.CreatedDate >= startDate.Value);
        }

        var totalCustomers = await customerQuery.CountAsync();
        var totalLeads = await leadQuery.CountAsync();
        var openLeads = await leadQuery.CountAsync(l => l.Status != "Lost" && l.Status != "Converted");

        var totalOpportunities = await oppQuery.CountAsync();
        var openOpportunities = await oppQuery.CountAsync(o => o.Status == "Open");
        var wonOpportunities = await oppQuery.CountAsync(o => o.Status == "Won");
        var lostOpportunities = await oppQuery.CountAsync(o => o.Status == "Lost");

        var openOpps = await oppQuery.Where(o => o.Status == "Open").ToListAsync();
        var totalPipelineValue = openOpps.Sum(o => o.Amount);
        var weightedPipelineValue = openOpps.Sum(o => o.WeightedAmount);

        // 1. Chart: Lead Status distribution
        var leadStatuses = new[] { "New", "Contacted", "Qualified", "Converted", "Lost" };
        var leadStatusLabels = new List<string>();
        var leadStatusCounts = new List<int>();

        var allLeadsInScope = await leadQuery.ToListAsync();
        foreach (var status in leadStatuses)
        {
            leadStatusLabels.Add(status);
            leadStatusCounts.Add(allLeadsInScope.Count(l => l.Status == status));
        }

        // 2. Chart: Opportunity Pipeline by Stage
        var pipelineStages = new[] { "Qualification", "Proposal", "Negotiation", "Won", "Lost" };
        var pipelineStageLabels = new List<string>();
        var pipelineStageAmounts = new List<decimal>();

        var allOppsInScope = await oppQuery.ToListAsync();
        foreach (var stage in pipelineStages)
        {
            pipelineStageLabels.Add(stage);
            pipelineStageAmounts.Add(allOppsInScope.Where(o => o.Stage == stage).Sum(o => o.Amount));
        }

        // 3. Chart: Monthly Sales (Last 6 Months)
        var monthlySalesLabels = new List<string>();
        var monthlySalesAmounts = new List<decimal>();
        for (int i = 5; i >= 0; i--)
        {
            var targetMonth = DateTime.UtcNow.AddMonths(-i);
            var monthName = targetMonth.ToString("MMM yyyy");
            monthlySalesLabels.Add(monthName);

            var monthStart = new DateTime(targetMonth.Year, targetMonth.Month, 1);
            var monthEnd = monthStart.AddMonths(1);

            var monthlyWon = allOppsInScope
                .Where(o => o.Status == "Won" && o.CreatedDate >= monthStart && o.CreatedDate < monthEnd)
                .Sum(o => o.Amount);

            monthlySalesAmounts.Add(monthlyWon);
        }

        // Recent items
        var recentActivities = await activityQuery
            .OrderByDescending(a => a.ActivityDate)
            .Take(5)
            .ToListAsync();

        var upcomingFollowUps = await followUpQuery
            .Where(f => f.Status == "Planned")
            .OrderBy(f => f.FollowUpDate)
            .Take(5)
            .ToListAsync();

        var highValueOpportunities = await oppQuery
            .Where(o => o.Status == "Open")
            .OrderByDescending(o => o.Amount)
            .Take(5)
            .ToListAsync();

        var model = new DashboardViewModel
        {
            TotalCustomers = totalCustomers,
            TotalLeads = totalLeads,
            OpenLeads = openLeads,
            TotalOpportunities = totalOpportunities,
            OpenOpportunities = openOpportunities,
            WonOpportunities = wonOpportunities,
            LostOpportunities = lostOpportunities,
            TotalPipelineValue = totalPipelineValue,
            WeightedPipelineValue = weightedPipelineValue,
            DateFilter = dateFilter,
            UserRole = userRole,
            UserFullName = userFullName,
            LeadStatusLabels = leadStatusLabels,
            LeadStatusCounts = leadStatusCounts,
            PipelineStageLabels = pipelineStageLabels,
            PipelineStageAmounts = pipelineStageAmounts,
            MonthlySalesLabels = monthlySalesLabels,
            MonthlySalesAmounts = monthlySalesAmounts,
            RecentActivities = recentActivities,
            UpcomingFollowUps = upcomingFollowUps,
            HighValueOpportunities = highValueOpportunities
        };

        return View(model);
    }
}
