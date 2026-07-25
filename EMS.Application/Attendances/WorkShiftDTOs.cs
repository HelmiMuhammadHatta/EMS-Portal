using System;

namespace EMS.Application.Attendances;

public record WorkShiftDto(
    Guid Id,
    string Name,
    TimeSpan StartTime,
    TimeSpan EndTime,
    bool IsOvernight,
    int ToleranceMinutes
);

public record CreateWorkShiftRequest(
    string Name,
    TimeSpan StartTime,
    TimeSpan EndTime,
    bool IsOvernight,
    int ToleranceMinutes
);

public record UpdateWorkShiftRequest(
    string Name,
    TimeSpan StartTime,
    TimeSpan EndTime,
    bool IsOvernight,
    int ToleranceMinutes
);

public record ShiftScheduleDto(
    Guid Id,
    Guid EmployeeId,
    DateTime Date,
    Guid WorkShiftId,
    WorkShiftDto WorkShift,
    bool IsManualOverride
);

public record AssignShiftScheduleRequest(
    Guid EmployeeId,
    DateTime Date,
    Guid WorkShiftId
);

public record BatchAssignShiftScheduleRequest(
    List<Guid> EmployeeIds,
    DateTime StartDate,
    DateTime EndDate,
    Guid WorkShiftId
);
