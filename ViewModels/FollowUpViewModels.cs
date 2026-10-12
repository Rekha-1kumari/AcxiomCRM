using System.ComponentModel.DataAnnotations;
using AcxiomCRM.Models;

namespace AcxiomCRM.ViewModels;

public class FollowUpInputModel
{
    public int FollowUpId { get; set; }

    [Display(Name = "Related Customer")]
    public int? CustomerId { get; set; }

    [Display(Name = "Related Lead")]
    public int? LeadId { get; set; }

    [Display(Name = "Related Opportunity")]
    public int? OpportunityId { get; set; }

    [Required(ErrorMessage = "Follow-up Date is required.")]
    [DataType(DataType.DateTime)]
    [Display(Name = "Follow-up Date & Time")]
    public DateTime FollowUpDate { get; set; } = DateTime.Today.AddHours(10);

    [Required(ErrorMessage = "Follow-up Type is required.")]
    [Display(Name = "Type")]
    public string FollowUpType { get; set; } = "Call"; // Call, Meeting, Email, Task

    [Required(ErrorMessage = "Remarks are required.")]
    [StringLength(500, ErrorMessage = "Remarks cannot exceed 500 characters.")]
    public string Remarks { get; set; } = string.Empty;

    [Required(ErrorMessage = "Status is required.")]
    public string Status { get; set; } = "Planned"; // Planned, Completed, Missed, Cancelled

    [Display(Name = "Assigned To")]
    public string? AssignedTo { get; set; }
}
