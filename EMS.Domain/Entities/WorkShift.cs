using System;

namespace EMS.Domain.Entities;

public class WorkShift
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public bool IsOvernight { get; set; }
    public int ToleranceMinutes { get; set; } = 15;
    
    // Audit fields
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
