using AcxiomCRM.Models;

namespace AcxiomCRM.ViewModels;

public class CustomerReportItem
{
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? OwnerName { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class LeadReportItem
{
    public string LeadCode { get; set; } = string.Empty;
    public string LeadName { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string Source { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal ExpectedValue { get; set; }
    public string? OwnerName { get; set; }
    public DateTime CreatedDate { get; set; }
    public bool IsConverted { get; set; }
}

public class FollowUpReportItem
{
    public int FollowUpId { get; set; }
    public DateTime FollowUpDate { get; set; }
    public string FollowUpType { get; set; } = string.Empty;
    public string Remarks { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? RelatedTo { get; set; }
    public string? AssignedUserName { get; set; }
    public bool IsOverdue { get; set; }
}

public class OpportunityReportItem
{
    public string OpportunityName { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public decimal Amount { get; set; }
    public string Stage { get; set; } = string.Empty;
    public int Probability { get; set; }
    public decimal WeightedAmount { get; set; }
    public DateTime ExpectedCloseDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? OwnerName { get; set; }
}

public class PipelineStageSummary
{
    public string Stage { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal WeightedAmount { get; set; }
}

public class UserActivitySummary
{
    public string UserName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public int CustomersCount { get; set; }
    public int LeadsCount { get; set; }
    public int OpportunitiesCount { get; set; }
    public int ActivitiesCount { get; set; }
    public int FollowUpsCount { get; set; }
}

public class ConversionSummary
{
    public int TotalLeads { get; set; }
    public int ConvertedLeads { get; set; }
    public int LostLeads { get; set; }
    public int OpenLeads { get; set; }
    public double ConversionRate => TotalLeads > 0 ? (double)ConvertedLeads / TotalLeads * 100 : 0;
}

public class ReportFilterViewModel
{
    public string ReportType { get; set; } = "Pipeline";
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Status { get; set; }
    public string? AssignedTo { get; set; }

    public List<CustomerReportItem> CustomerReports { get; set; } = new();
    public List<LeadReportItem> LeadReports { get; set; } = new();
    public List<FollowUpReportItem> FollowUpReports { get; set; } = new();
    public List<OpportunityReportItem> OpportunityReports { get; set; } = new();
    public List<PipelineStageSummary> PipelineSummaries { get; set; } = new();
    public List<UserActivitySummary> UserActivities { get; set; } = new();
    public ConversionSummary ConversionSummary { get; set; } = new();
    public List<AuditLog> AuditLogs { get; set; } = new();
}
