using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EMS.Application.Attendances;

public interface IWorkShiftService
{
    // WorkShift CRUD
    Task<List<WorkShiftDto>> GetAllWorkShiftsAsync();
    Task<WorkShiftDto?> GetWorkShiftByIdAsync(Guid id);
    Task<WorkShiftDto> CreateWorkShiftAsync(CreateWorkShiftRequest request);
    Task<WorkShiftDto> UpdateWorkShiftAsync(Guid id, UpdateWorkShiftRequest request);
    Task DeleteWorkShiftAsync(Guid id);

    // ShiftSchedule
    Task<List<ShiftScheduleDto>> GetShiftSchedulesAsync(Guid? employeeId, DateTime startDate, DateTime endDate);
    Task AssignShiftScheduleAsync(AssignShiftScheduleRequest request, Guid createdByUserId);
    Task OverrideShiftAsync(Guid id, Guid workShiftId);
}
