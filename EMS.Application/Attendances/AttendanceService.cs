using ClosedXML.Excel;
using EMS.Application.Employees;
using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using EMS.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace EMS.Application.Attendances;

public class AttendanceService : IAttendanceService
{
    private readonly IApplicationDbContext _context;

    public AttendanceService(IApplicationDbContext context)
    {
        _context = context;
    }

    private double CalculateDistanceMeters(double lat1, double lon1, double lat2, double lon2)
    {
        var r = 6371e3; // metres
        var phi1 = lat1 * Math.PI / 180;
        var phi2 = lat2 * Math.PI / 180;
        var deltaPhi = (lat2 - lat1) * Math.PI / 180;
        var deltaLambda = (lon2 - lon1) * Math.PI / 180;

        var a = Math.Sin(deltaPhi / 2) * Math.Sin(deltaPhi / 2) +
                Math.Cos(phi1) * Math.Cos(phi2) *
                Math.Sin(deltaLambda / 2) * Math.Sin(deltaLambda / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return r * c;
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

    public async Task ClockInAsync(ClockInRequest request, Guid requesterUserId)
    {
        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == requesterUserId);
        if (employee == null) throw new Exception("Employee profile not found.");

        // Removed distance check
        
        string? photoPath = null;
        if (!string.IsNullOrEmpty(request.PhotoBase64))
        {
            try
            {
                var base64Data = request.PhotoBase64.Substring(request.PhotoBase64.IndexOf(",") + 1);
                var bytes = Convert.FromBase64String(base64Data);
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "attendances");
                Directory.CreateDirectory(uploadsFolder);
                var fileName = $"{Guid.NewGuid()}.jpg";
                var filePath = Path.Combine(uploadsFolder, fileName);
                await File.WriteAllBytesAsync(filePath, bytes);
                photoPath = $"/uploads/attendances/{fileName}";
            }
            catch (Exception)
            {
                // Ignore invalid base64
            }
        }

        var now = DateTime.UtcNow;
        var localTime = now.AddHours(7); // Assume UTC+7 for Jakarta
        var today = localTime.Date;

        var attendances = await _context.Attendances
            .Where(a => a.EmployeeId == employee.Id)
            .ToListAsync();
            
        var existing = attendances.FirstOrDefault(a => a.ClockIn.AddHours(7).Date == today);

        if (existing != null)
            throw new Exception("You have already clocked in today.");

        var status = AttendanceStatus.OnTime;
        var lateThreshold = new TimeSpan(8, 15, 0); // 08:15 AM
        if (localTime.TimeOfDay > lateThreshold)
        {
            status = AttendanceStatus.Late;
        }

        var attendance = new Attendance
        {
            Id = Guid.NewGuid(),
            EmployeeId = employee.Id,
            ClockIn = now,
            Latitude = (decimal)request.Latitude,
            Longitude = (decimal)request.Longitude,
            Status = status,
            LocationName = request.LocationName,
            PhotoPath = photoPath,
            CreatedAt = DateTime.UtcNow
        };

        _context.Attendances.Add(attendance);
        await _context.SaveChangesAsync();
    }

    public async Task ClockOutAsync(ClockOutRequest request, Guid requesterUserId)
    {
        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == requesterUserId);
        if (employee == null) throw new Exception("Employee profile not found.");

        // Removed distance check
        
        string? photoPath = null;
        if (!string.IsNullOrEmpty(request.PhotoBase64))
        {
            try
            {
                var base64Data = request.PhotoBase64.Substring(request.PhotoBase64.IndexOf(",") + 1);
                var bytes = Convert.FromBase64String(base64Data);
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "attendances");
                Directory.CreateDirectory(uploadsFolder);
                var fileName = $"{Guid.NewGuid()}.jpg";
                var filePath = Path.Combine(uploadsFolder, fileName);
                await File.WriteAllBytesAsync(filePath, bytes);
                photoPath = $"/uploads/attendances/{fileName}";
            }
            catch (Exception)
            {
                // Ignore invalid base64
            }
        }

        var now = DateTime.UtcNow;
        var localTime = now.AddHours(7);
        var today = localTime.Date;

        // Note: this avoids client side eval exception by querying all for employee, assuming one per day usually
        var attendances = await _context.Attendances.Where(a => a.EmployeeId == employee.Id).ToListAsync();
        var existing = attendances.FirstOrDefault(a => a.ClockIn.AddHours(7).Date == today);

        if (existing == null)
            throw new Exception("You haven't clocked in today.");

        if (existing.ClockOut != null)
            throw new Exception("You have already clocked out today.");

        if (localTime.TimeOfDay < new TimeSpan(17, 0, 0)) // 17:00 PM
        {
            existing.Status = AttendanceStatus.EarlyLeave;
        }

        existing.ClockOut = now;
        if (!string.IsNullOrEmpty(request.LocationName))
        {
            existing.LocationName = request.LocationName; // Can update or just append
        }
        if (photoPath != null)
        {
            existing.PhotoPath = photoPath; // If we want to overwrite with clock-out photo
        }
        
        await _context.SaveChangesAsync();
    }

    public async Task<PaginatedResponse<AttendanceResponse>> GetAttendancesAsync(AttendanceListQuery query, Guid requesterUserId, bool isRequesterAdmin)
    {
        var dbQuery = _context.Attendances.Include(a => a.Employee).AsQueryable();

        var allowedIds = await GetAllowedEmployeeIdsAsync(requesterUserId, isRequesterAdmin);
        if (allowedIds != null)
        {
            dbQuery = dbQuery.Where(a => allowedIds.Contains(a.EmployeeId));
        }

        if (query.EmployeeId.HasValue)
            dbQuery = dbQuery.Where(a => a.EmployeeId == query.EmployeeId.Value);

        if (query.StartDate.HasValue)
        {
            var startUtc = query.StartDate.Value.ToUniversalTime();
            dbQuery = dbQuery.Where(a => a.ClockIn >= startUtc);
        }

        if (query.EndDate.HasValue)
        {
            var endUtc = query.EndDate.Value.ToUniversalTime();
            dbQuery = dbQuery.Where(a => a.ClockIn <= endUtc);
        }

        if (!string.IsNullOrEmpty(query.Status) && Enum.TryParse<AttendanceStatus>(query.Status, true, out var status))
            dbQuery = dbQuery.Where(a => a.Status == status);

        dbQuery = dbQuery.OrderByDescending(a => a.ClockIn);

        var totalCount = await dbQuery.CountAsync();
        var items = await dbQuery.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync();

        return new PaginatedResponse<AttendanceResponse>
        {
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)query.PageSize),
            CurrentPage = query.Page,
            Data = items.Select(a => new AttendanceResponse
            {
                Id = a.Id,
                EmployeeId = a.EmployeeId,
                EmployeeName = a.Employee?.FullName ?? "",
                ClockIn = a.ClockIn,
                ClockOut = a.ClockOut,
                Status = a.Status.ToString(),
                LocationName = a.LocationName,
                PhotoUrl = a.PhotoPath
            })
        };
    }

    public async Task<AttendanceSummaryResponse> GetSummaryAsync(Guid employeeId, int month, int year, Guid requesterUserId, bool isRequesterAdmin)
    {
        var allowedIds = await GetAllowedEmployeeIdsAsync(requesterUserId, isRequesterAdmin);
        if (allowedIds != null && !allowedIds.Contains(employeeId))
            throw new Exception("Forbidden: Cannot view attendance for this employee.");

        var employee = await _context.Employees.FindAsync(employeeId);
        if (employee == null) throw new Exception("Employee not found.");

        var attendances = await _context.Attendances
            .Where(a => a.EmployeeId == employeeId)
            .ToListAsync();

        // Filter in memory for local time
        var monthlyAttendances = attendances
            .Where(a => a.ClockIn.AddHours(7).Month == month && a.ClockIn.AddHours(7).Year == year)
            .ToList();

        double totalHours = 0;
        foreach (var a in monthlyAttendances)
        {
            if (a.ClockOut.HasValue)
            {
                totalHours += (a.ClockOut.Value - a.ClockIn).TotalHours;
            }
        }

        return new AttendanceSummaryResponse
        {
            EmployeeId = employeeId,
            EmployeeName = employee.FullName,
            Month = month,
            Year = year,
            TotalPresent = monthlyAttendances.Count(a => a.Status == AttendanceStatus.OnTime),
            TotalLate = monthlyAttendances.Count(a => a.Status == AttendanceStatus.Late),
            TotalEarlyLeave = monthlyAttendances.Count(a => a.Status == AttendanceStatus.EarlyLeave),
            TotalAbsent = 0, // Absent would be calculated based on working days minus total attendances + leaves
            TotalWorkingHours = Math.Round(totalHours, 2)
        };
    }

    public async Task<byte[]> ExportAttendancesAsync(DateTime? startDate, DateTime? endDate, Guid? departmentId, Guid requesterUserId, bool isRequesterAdmin)
    {
        var dbQuery = _context.Attendances
            .Include(a => a.Employee)
            .ThenInclude(e => e.Department)
            .AsQueryable();

        var allowedIds = await GetAllowedEmployeeIdsAsync(requesterUserId, isRequesterAdmin);
        if (allowedIds != null)
        {
            dbQuery = dbQuery.Where(a => allowedIds.Contains(a.EmployeeId));
        }

        if (startDate.HasValue)
            dbQuery = dbQuery.Where(a => a.ClockIn >= startDate.Value.ToUniversalTime());
            
        if (endDate.HasValue)
            dbQuery = dbQuery.Where(a => a.ClockIn <= endDate.Value.ToUniversalTime());

        if (departmentId.HasValue)
            dbQuery = dbQuery.Where(a => a.Employee.DepartmentId == departmentId.Value);

        var attendances = await dbQuery.OrderByDescending(a => a.ClockIn).ToListAsync();

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Attendances");
        
        ws.Cell(1, 1).Value = "Date";
        ws.Cell(1, 2).Value = "Employee Name";
        ws.Cell(1, 3).Value = "Department";
        ws.Cell(1, 4).Value = "Clock In";
        ws.Cell(1, 5).Value = "Clock Out";
        ws.Cell(1, 6).Value = "Status";

        ws.Range("A1:F1").Style.Font.Bold = true;

        int row = 2;
        foreach (var a in attendances)
        {
            ws.Cell(row, 1).Value = a.ClockIn.AddHours(7).ToString("yyyy-MM-dd");
            ws.Cell(row, 2).Value = a.Employee?.FullName;
            ws.Cell(row, 3).Value = a.Employee?.Department?.Name;
            ws.Cell(row, 4).Value = a.ClockIn.AddHours(7).ToString("HH:mm:ss");
            ws.Cell(row, 5).Value = a.ClockOut?.AddHours(7).ToString("HH:mm:ss") ?? "-";
            ws.Cell(row, 6).Value = a.Status.ToString();
            row++;
        }

        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
