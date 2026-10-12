using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.Models;

public class AuditLog
{
    public int AuditLogId { get; set; }

    [StringLength(100)]
    public string? UserId { get; set; }

    [StringLength(150)]
    public string? UserName { get; set; }

    [Required]
    [StringLength(50)]
    public string Action { get; set; } = string.Empty; // Login, Failed Login, Logout, Create, Update, Delete, Role Change, Security

    [Required]
    [StringLength(50)]
    public string EntityName { get; set; } = string.Empty; // Customer, Lead, Opportunity, FollowUp, Activity, User, Auth

    [StringLength(50)]
    public string? RecordId { get; set; }

    public string? OldValue { get; set; }

    public string? NewValue { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    [StringLength(50)]
    public string? IpAddress { get; set; }
}
