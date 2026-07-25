using System;

namespace EMS.Domain.Entities;

public class ShiftSchedule
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;
    
    // We use DateTime but treat it as a Date (time part should be 00:00:00)
    public DateTime Date { get; set; }
    
    public Guid WorkShiftId { get; set; }
    public WorkShift WorkShift { get; set; } = null!;
    
    public bool IsManualOverride { get; set; } = false;
    
    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
}
