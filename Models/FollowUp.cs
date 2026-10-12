using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AcxiomCRM.Models;

public class FollowUp
{
    public int FollowUpId { get; set; }

    public int? CustomerId { get; set; }
    [ForeignKey("CustomerId")]
    public virtual Customer? Customer { get; set; }

    public int? LeadId { get; set; }
    [ForeignKey("LeadId")]
    public virtual Lead? Lead { get; set; }

    public int? OpportunityId { get; set; }
    [ForeignKey("OpportunityId")]
    public virtual Opportunity? Opportunity { get; set; }

    [Required]
    [DataType(DataType.DateTime)]
    public DateTime FollowUpDate { get; set; }

    [Required]
    [StringLength(50)]
    public string FollowUpType { get; set; } = "Call"; // Call, Meeting, Email, Task

    [Required]
    [StringLength(500)]
    public string Remarks { get; set; } = string.Empty;

    [Required]
    [StringLength(30)]
    public string Status { get; set; } = "Planned"; // Planned, Completed, Missed, Cancelled

    public string? AssignedTo { get; set; } // UserId
}
