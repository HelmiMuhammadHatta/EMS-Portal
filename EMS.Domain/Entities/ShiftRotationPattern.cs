using System;

namespace EMS.Domain.Entities;

public class ShiftRotationPattern
{
    public Guid Id { get; set; }
    
    public Guid RotationGroupId { get; set; }
    public ShiftRotationGroup RotationGroup { get; set; } = null!;
    
    public int CycleWeekNumber { get; set; } // 1, 2, 3...
    
    public Guid WorkShiftId { get; set; }
    public WorkShift WorkShift { get; set; } = null!;
}
