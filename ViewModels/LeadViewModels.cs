using System.ComponentModel.DataAnnotations;
using AcxiomCRM.Models;

namespace AcxiomCRM.ViewModels;

public class LeadInputModel
{
    public int LeadId { get; set; }

    [Display(Name = "Lead Code")]
    public string? LeadCode { get; set; }

    [Required(ErrorMessage = "Lead Name is required.")]
    [StringLength(100, ErrorMessage = "Lead Name cannot exceed 100 characters.")]
    [Display(Name = "Lead Name")]
    public string LeadName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters.")]
    [Display(Name = "Email Address")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone is required.")]
    [RegularExpression(@"^[0-9]{10}$", ErrorMessage = "Enter a valid 10-digit phone number.")]
    [Display(Name = "Phone Number")]
    public string Phone { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "Company Name cannot exceed 100 characters.")]
    [Display(Name = "Company Name")]
    public string? CompanyName { get; set; }

    [Required(ErrorMessage = "Lead Source is required.")]
    public string Source { get; set; } = "Website";

    [Required(ErrorMessage = "Lead Status is required.")]
    public string Status { get; set; } = "New"; // New, Contacted, Qualified, Unqualified, Converted, Lost

    [Required(ErrorMessage = "Expected Value is required.")]
    [Range(0, 100000000, ErrorMessage = "Expected Value must be between 0 and 100,000,000.")]
    [Display(Name = "Expected Deal Value (₹/$)")]
    public decimal ExpectedValue { get; set; }

    [Display(Name = "Assigned Sales Executive")]
    public string? AssignedTo { get; set; }
}

public class LeadConvertViewModel
{
    public int LeadId { get; set; }

    [Display(Name = "Lead Name")]
    public string LeadName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? CompanyName { get; set; }

    [Display(Name = "Create Customer Record")]
    public bool CreateCustomer { get; set; } = true;

    [Display(Name = "Create Sales Opportunity")]
    public bool CreateOpportunity { get; set; } = true;

    [Display(Name = "Opportunity Name")]
    [StringLength(150)]
    public string? OpportunityName { get; set; }

    [Range(0.01, 1000000000, ErrorMessage = "Opportunity Amount must be greater than 0.")]
    [Display(Name = "Opportunity Amount")]
    public decimal OpportunityAmount { get; set; }

    [Display(Name = "Initial Stage")]
    public string OpportunityStage { get; set; } = "Proposal";

    [Range(0, 100, ErrorMessage = "Probability must be between 0 and 100.")]
    public int Probability { get; set; } = 50;

    [DataType(DataType.Date)]
    [Display(Name = "Expected Close Date")]
    public DateTime ExpectedCloseDate { get; set; } = DateTime.Today.AddDays(30);
}

public class LeadDetailsViewModel
{
    public Lead Lead { get; set; } = null!;
    public string? AssignedUserName { get; set; }
    public Customer? ConvertedCustomer { get; set; }
    public Opportunity? ConvertedOpportunity { get; set; }
    public List<FollowUp> FollowUps { get; set; } = new();
    public List<Activity> Activities { get; set; } = new();
}
