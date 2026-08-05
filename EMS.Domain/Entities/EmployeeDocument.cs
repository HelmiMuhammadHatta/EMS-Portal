using EMS.Domain.Enums;

namespace EMS.Domain.Entities;

public class EmployeeDocument
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;
    public DocumentType DocumentType { get; set; }
    public string FilePath { get; set; } = null!;
    public string FileName { get; set; } = null!;
    public long FileSize { get; set; }
    public Guid? UploadedBy { get; set; }
    public DateTime UploadedAt { get; set; }
}
