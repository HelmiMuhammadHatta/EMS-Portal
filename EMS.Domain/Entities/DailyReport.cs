using EMS.Domain.Enums;

namespace EMS.Domain.Entities;

public class DailyReport
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;
    
    public DateTime ReportDate { get; set; }
    
    public string TasksCompleted { get; set; } = null!;
    public string? Blockers { get; set; }
    
    public DailyReportStatus Status { get; set; }
    
    public string? ManagerFeedback { get; set; }
    public Guid? ReviewedBy { get; set; }
    public Employee? Reviewer { get; set; }
    public DateTime? ReviewedAt { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
