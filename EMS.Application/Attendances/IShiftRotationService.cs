using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EMS.Application.Attendances;

public interface IShiftRotationService
{
    // Rotation Groups
    Task<List<ShiftRotationGroupDto>> GetAllGroupsAsync();
    Task<ShiftRotationGroupDto?> GetGroupByIdAsync(Guid id);
    Task<ShiftRotationGroupDto> CreateGroupAsync(CreateShiftRotationGroupRequest request);
    Task<ShiftRotationGroupDto> UpdateGroupAsync(Guid id, UpdateShiftRotationGroupRequest request);
    Task DeleteGroupAsync(Guid id);
    Task AssignEmployeesToGroupAsync(Guid groupId, List<Guid> employeeIds);
    
    // Rotation Patterns
    Task<List<ShiftRotationPatternDto>> GetPatternsByGroupIdAsync(Guid groupId);
    Task<ShiftRotationPatternDto> CreatePatternAsync(CreateShiftRotationPatternRequest request);
    Task<ShiftRotationPatternDto> UpdatePatternAsync(Guid id, UpdateShiftRotationPatternRequest request);
    Task DeletePatternAsync(Guid id);
    
    // Generator logic
    Task GenerateShiftScheduleAsync(DateTime startDate, DateTime endDate);
}
