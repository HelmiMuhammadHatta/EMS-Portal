using System;
using System.Threading.Tasks;
using EMS.Application.Employees;

namespace EMS.Application.DailyReports;

public interface IDailyReportService
{
    Task<PaginatedResponse<DailyReportDto>> GetDailyReportsAsync(DailyReportListQuery query, Guid requesterUserId, bool isRequesterAdmin);
    Task<DailyReportDto> GetDailyReportByIdAsync(Guid id, Guid requesterUserId, bool isRequesterAdmin);
    Task<DailyReportDto> CreateDailyReportAsync(CreateDailyReportDto request, Guid requesterUserId);
    Task<DailyReportDto> UpdateDailyReportAsync(Guid id, UpdateDailyReportDto request, Guid requesterUserId);
    Task<DailyReportDto> ReviewDailyReportAsync(Guid id, ReviewDailyReportDto request, Guid requesterUserId);
    Task DeleteDailyReportAsync(Guid id, Guid requesterUserId);
}
