using System;
using System.Collections.Generic;

namespace EMS.Application.Attendances;

public record ShiftRotationGroupDto(Guid Id, string Name, DateTime RotationStartDate);
public record CreateShiftRotationGroupRequest(string Name, DateTime RotationStartDate);
public record UpdateShiftRotationGroupRequest(string Name, DateTime RotationStartDate);

public record ShiftRotationPatternDto(Guid Id, Guid RotationGroupId, int CycleWeekNumber, Guid WorkShiftId, string WorkShiftName);
public record CreateShiftRotationPatternRequest(Guid RotationGroupId, int CycleWeekNumber, Guid WorkShiftId);
public record UpdateShiftRotationPatternRequest(int CycleWeekNumber, Guid WorkShiftId);
