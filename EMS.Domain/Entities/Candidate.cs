using System;
using EMS.Domain.Enums;

namespace EMS.Domain.Entities;

public class Candidate
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    
    public Guid AppliedDepartmentId { get; set; }
    public Department AppliedDepartment { get; set; } = null!;
    
    public Guid AppliedPositionId { get; set; }
    public Position AppliedPosition { get; set; } = null!;
    
    public CandidateStatus Status { get; set; }
    public string? Notes { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
