using EMS.Domain.Enums;

namespace EMS.Domain.Entities;

public class AuditLog
{
    public Guid Id { get; set; }
    public string TableName { get; set; } = null!;
    public string RecordId { get; set; } = null!;
    public AuditAction Action { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public Guid? ChangedBy { get; set; }
    public User? ChangedByUser { get; set; }
    public DateTime ChangedAt { get; set; }
}
