using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EMS.Application.Employees;
using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using EMS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EMS.Application.DailyReports;

public class DailyReportService : IDailyReportService
{
    private readonly IApplicationDbContext _context;

    public DailyReportService(IApplicationDbContext context)
    {
        _context = context;
    }

    private async Task<List<Guid>?> GetAllowedEmployeeIdsAsync(Guid requesterUserId, bool isRequesterAdmin)
    {
        if (isRequesterAdmin) return null;
        
        var requesterEmployee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == requesterUserId);
        if (requesterEmployee == null) return new List<Guid> { Guid.Empty };

        var allEmployees = await _context.Employees.Select(e => new { e.Id, e.ManagerId }).ToListAsync();
        var allowedIds = new HashSet<Guid>();
        
        void CollectSubordinates(Guid managerId)
        {
            var subs = allEmployees.Where(e => e.ManagerId == managerId).Select(e => e.Id).ToList();
            foreach (var sub in subs)
            {
                allowedIds.Add(sub);
                CollectSubordinates(sub);
            }
        }
        
        allowedIds.Add(requesterEmployee.Id);
        CollectSubordinates(requesterEmployee.Id);

        return allowedIds.ToList();
    }

    public async Task<PaginatedResponse<DailyReportDto>> GetDailyReportsAsync(DailyReportListQuery query, Guid requesterUserId, bool isRequesterAdmin)
    {
        var dbQuery = _context.DailyReports
            .Include(dr => dr.Employee)
            .Include(dr => dr.Reviewer)
            .AsQueryable();

        var allowedIds = await GetAllowedEmployeeIdsAsync(requesterUserId, isRequesterAdmin);
        if (allowedIds != null)
        {
            dbQuery = dbQuery.Where(dr => allowedIds.Contains(dr.EmployeeId));
        }

        if (query.EmployeeId.HasValue)
            dbQuery = dbQuery.Where(dr => dr.EmployeeId == query.EmployeeId.Value);

        if (query.StartDate.HasValue)
        {
            var startUtc = DateTime.SpecifyKind(query.StartDate.Value.Date, DateTimeKind.Utc);
            dbQuery = dbQuery.Where(dr => dr.ReportDate >= startUtc);
        }

        if (query.EndDate.HasValue)
        {
            var endUtc = DateTime.SpecifyKind(query.EndDate.Value.Date, DateTimeKind.Utc);
            dbQuery = dbQuery.Where(dr => dr.ReportDate <= endUtc);
        }

        if (!string.IsNullOrEmpty(query.Status) && Enum.TryParse<DailyReportStatus>(query.Status, true, out var status))
            dbQuery = dbQuery.Where(dr => dr.Status == status);

        dbQuery = dbQuery.OrderByDescending(dr => dr.ReportDate).ThenByDescending(dr => dr.CreatedAt);

        var totalCount = await dbQuery.CountAsync();
        var items = await dbQuery.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync();

        return new PaginatedResponse<DailyReportDto>
        {
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)query.PageSize),
            CurrentPage = query.Page,
            Data = items.Select(dr => new DailyReportDto
            {
                Id = dr.Id,
                EmployeeId = dr.EmployeeId,
                EmployeeName = dr.Employee?.FullName ?? string.Empty,
                ReportDate = dr.ReportDate,
                TasksCompleted = dr.TasksCompleted,
                Blockers = dr.Blockers,
                Status = dr.Status.ToString(),
                ManagerFeedback = dr.ManagerFeedback,
                ReviewedBy = dr.ReviewedBy,
                ReviewerName = dr.Reviewer?.FullName,
                ReviewedAt = dr.ReviewedAt,
                CreatedAt = dr.CreatedAt
            })
        };
    }

    public async Task<DailyReportDto> GetDailyReportByIdAsync(Guid id, Guid requesterUserId, bool isRequesterAdmin)
    {
        var report = await _context.DailyReports
            .Include(dr => dr.Employee)
            .Include(dr => dr.Reviewer)
            .FirstOrDefaultAsync(dr => dr.Id == id);

        if (report == null)
            throw new Exception("Daily report not found.");

        var allowedIds = await GetAllowedEmployeeIdsAsync(requesterUserId, isRequesterAdmin);
        if (allowedIds != null && !allowedIds.Contains(report.EmployeeId))
            throw new Exception("Forbidden: You cannot access this daily report.");

        return new DailyReportDto
        {
            Id = report.Id,
            EmployeeId = report.EmployeeId,
            EmployeeName = report.Employee?.FullName ?? string.Empty,
            ReportDate = report.ReportDate,
            TasksCompleted = report.TasksCompleted,
            Blockers = report.Blockers,
            Status = report.Status.ToString(),
            ManagerFeedback = report.ManagerFeedback,
            ReviewedBy = report.ReviewedBy,
            ReviewerName = report.Reviewer?.FullName,
            ReviewedAt = report.ReviewedAt,
            CreatedAt = report.CreatedAt
        };
    }

    public async Task<DailyReportDto> CreateDailyReportAsync(CreateDailyReportDto request, Guid requesterUserId)
    {
        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == requesterUserId);
        if (employee == null) throw new Exception("Employee profile not found.");

        var reportDate = request.ReportDate.Date;

        // Check if attendance exists for the day (Convert local day to UTC range to avoid timezone mismatch)
        var startOfDayUtc = reportDate.AddHours(-7); // Assuming UTC+7 for Jakarta
        var endOfDayUtc = startOfDayUtc.AddDays(1);
        var hasClockedIn = await _context.Attendances.AnyAsync(a => 
            a.EmployeeId == employee.Id && 
            a.ClockIn >= startOfDayUtc && 
            a.ClockIn < endOfDayUtc);
        if (!hasClockedIn)
        {
            throw new Exception("Anda belum melakukan clock-in hari ini. Laporan harian hanya bisa dibuat jika sudah clock-in.");
        }

        // Check if report already exists for the day
        var existingReport = await _context.DailyReports.AnyAsync(dr => dr.EmployeeId == employee.Id && dr.ReportDate == reportDate);
        if (existingReport)
        {
            throw new Exception("Anda sudah membuat laporan harian untuk tanggal tersebut.");
        }

        var report = new DailyReport
        {
            Id = Guid.NewGuid(),
            EmployeeId = employee.Id,
            ReportDate = reportDate,
            TasksCompleted = request.TasksCompleted,
            Blockers = request.Blockers,
            Status = DailyReportStatus.Submitted,
            CreatedAt = DateTime.UtcNow
        };

        _context.DailyReports.Add(report);
        await _context.SaveChangesAsync();

        return await GetDailyReportByIdAsync(report.Id, requesterUserId, false);
    }

    public async Task<DailyReportDto> UpdateDailyReportAsync(Guid id, UpdateDailyReportDto request, Guid requesterUserId)
    {
        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == requesterUserId);
        if (employee == null) throw new Exception("Employee profile not found.");

        var report = await _context.DailyReports.FirstOrDefaultAsync(dr => dr.Id == id);
        if (report == null) throw new Exception("Daily report not found.");

        if (report.EmployeeId != employee.Id)
            throw new Exception("Forbidden: You can only edit your own daily reports.");

        if (report.Status != DailyReportStatus.Submitted)
            throw new Exception("Cannot edit a report that has already been reviewed.");

        report.TasksCompleted = request.TasksCompleted;
        report.Blockers = request.Blockers;
        report.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return await GetDailyReportByIdAsync(report.Id, requesterUserId, false);
    }

    public async Task<DailyReportDto> ReviewDailyReportAsync(Guid id, ReviewDailyReportDto request, Guid requesterUserId)
    {
        var reviewer = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == requesterUserId);
        if (reviewer == null) throw new Exception("Employee profile not found.");

        var report = await _context.DailyReports.FirstOrDefaultAsync(dr => dr.Id == id);
        if (report == null) throw new Exception("Daily report not found.");

        // Validasi: Manager hanya bisa melihat dan memberi feedback pada laporan milik bawahan langsungnya (gunakan hierarchy ManagerId yang sudah ada)
        // or Admin (if we consider admin can review, but requirement says manager)
        var allowedIds = await GetAllowedEmployeeIdsAsync(requesterUserId, false); 
        if (allowedIds == null || !allowedIds.Contains(report.EmployeeId))
        {
            throw new Exception("Forbidden: You can only review reports of your subordinates.");
        }

        report.ManagerFeedback = request.ManagerFeedback;
        report.Status = DailyReportStatus.Reviewed;
        report.ReviewedBy = reviewer.Id;
        report.ReviewedAt = DateTime.UtcNow;
        report.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return await GetDailyReportByIdAsync(report.Id, requesterUserId, false);
    }

    public async Task DeleteDailyReportAsync(Guid id, Guid requesterUserId)
    {
        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == requesterUserId);
        if (employee == null) throw new Exception("Employee profile not found.");

        var report = await _context.DailyReports.FirstOrDefaultAsync(dr => dr.Id == id);
        if (report == null) throw new Exception("Daily report not found.");

        if (report.EmployeeId != employee.Id)
            throw new Exception("Forbidden: You can only delete your own daily reports.");

        if (report.Status != DailyReportStatus.Submitted)
            throw new Exception("Cannot delete a report that has already been reviewed.");

        _context.DailyReports.Remove(report);
        await _context.SaveChangesAsync();
    }
}
