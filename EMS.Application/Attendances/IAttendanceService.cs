using EMS.Application.Employees;

namespace EMS.Application.Attendances;

public interface IAttendanceService
{
    Task ClockInAsync(ClockInRequest request, Guid requesterUserId);
    Task ClockOutAsync(ClockOutRequest request, Guid requesterUserId);
    Task<PaginatedResponse<AttendanceResponse>> GetAttendancesAsync(AttendanceListQuery query, Guid requesterUserId, bool isRequesterAdmin);
    Task<AttendanceSummaryResponse> GetSummaryAsync(Guid employeeId, int month, int year, Guid requesterUserId, bool isRequesterAdmin);
    Task<byte[]> ExportAttendancesAsync(DateTime? startDate, DateTime? endDate, Guid? departmentId, Guid requesterUserId, bool isRequesterAdmin);
}
