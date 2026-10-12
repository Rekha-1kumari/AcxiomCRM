using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.DTOs;
using AcxiomCRM.Models;

namespace AcxiomCRM.Controllers.Api;

[ApiController]
[Route("api/reports")]
[Authorize]
public class ReportsApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ReportsApiController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: /api/reports/pipeline
    [HttpGet("pipeline")]
    public async Task<ActionResult<IEnumerable<PipelineReportDto>>> GetPipelineReport()
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isSalesExec = User.IsInRole("SalesExecutive");

        IQueryable<Opportunity> query = _context.Opportunities;
        if (isSalesExec && !string.IsNullOrEmpty(currentUserId))
        {
            query = query.Where(o => o.AssignedTo == currentUserId);
        }

        var opps = await query.ToListAsync();
        var stages = new[] { "Qualification", "Proposal", "Negotiation", "Won", "Lost" };

        var list = new List<PipelineReportDto>();
        foreach (var stage in stages)
        {
            var stageOpps = opps.Where(o => o.Stage == stage).ToList();
            list.Add(new PipelineReportDto
            {
                Stage = stage,
                Count = stageOpps.Count,
                TotalAmount = stageOpps.Sum(o => o.Amount),
                WeightedAmount = stageOpps.Sum(o => o.WeightedAmount)
            });
        }

        return Ok(list);
    }
}
