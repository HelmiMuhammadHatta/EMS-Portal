using EMS.Application.Employees;
using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using EMS.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EMS.Application.Leaves;

public class LeaveService : ILeaveService
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<LeaveService> _logger;

    public LeaveService(IApplicationDbContext context, ILogger<LeaveService> logger)
    {
        _context = context;
        _logger = logger;
    }

    private int CalculateLeaveDays(DateTime start, DateTime end)
    {
        int days = 0;
        var current = start.Date;
        while (current <= end.Date)
        {
            if (current.DayOfWeek != DayOfWeek.Saturday && current.DayOfWeek != DayOfWeek.Sunday)
                days++;
            current = current.AddDays(1);
        }
        return days;
    }

    private async Task<List<Guid>?> GetAllowedEmployeeIdsAsync(Guid requesterUserId, bool isRequesterAdmin)
    {
        if (isRequesterAdmin) return null; // null means all allowed
        
        var requesterEmployee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == requesterUserId);
        if (requesterEmployee == null) return new List<Guid> { Guid.Empty }; // allowed none

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

    public async Task<IEnumerable<LeaveTypeResponse>> GetLeaveTypesAsync(Guid? employeeId = null)
    {
        var types = await _context.LeaveTypes.ToListAsync();

        if (employeeId.HasValue)
        {
            var employee = await _context.Employees.FindAsync(employeeId.Value);
            if (employee != null)
            {
                types = types.Where(t => t.EligibleGender == null || t.EligibleGender == employee.Gender).ToList();
            }
        }

        return types.Select(t => new LeaveTypeResponse
        {
            Id = t.Id,
            Name = t.Name,
            DefaultDaysPerYear = t.DefaultDaysPerYear
        });
    }

    public async Task<PaginatedResponse<LeaveRequestResponse>> GetLeaveRequestsAsync(LeaveRequestListQuery query, Guid requesterUserId, bool isRequesterAdmin)
    {
        var dbQuery = _context.LeaveRequests
            .Include(lr => lr.Employee)
            .Include(lr => lr.LeaveType)
            .Include(lr => lr.Approver)
            .AsQueryable();

        var allowedIds = await GetAllowedEmployeeIdsAsync(requesterUserId, isRequesterAdmin);
        if (allowedIds != null)
        {
            dbQuery = dbQuery.Where(lr => allowedIds.Contains(lr.EmployeeId));
        }

        if (query.EmployeeId.HasValue)
            dbQuery = dbQuery.Where(lr => lr.EmployeeId == query.EmployeeId.Value);

        if (!string.IsNullOrEmpty(query.Status) && Enum.TryParse<LeaveRequestStatus>(query.Status, true, out var status))
            dbQuery = dbQuery.Where(lr => lr.Status == status);

        if (query.StartDate.HasValue)
        {
            var startUtc = DateTime.SpecifyKind(query.StartDate.Value, DateTimeKind.Utc);
            dbQuery = dbQuery.Where(lr => lr.StartDate >= startUtc);
        }

        if (query.EndDate.HasValue)
        {
            var endUtc = DateTime.SpecifyKind(query.EndDate.Value, DateTimeKind.Utc);
            dbQuery = dbQuery.Where(lr => lr.EndDate <= endUtc);
        }

        dbQuery = dbQuery.OrderByDescending(lr => lr.CreatedAt);

        var totalCount = await dbQuery.CountAsync();
        var items = await dbQuery.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync();

        return new PaginatedResponse<LeaveRequestResponse>
        {
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)query.PageSize),
            CurrentPage = query.Page,
            Data = items.Select(lr => new LeaveRequestResponse
            {
                Id = lr.Id,
                EmployeeId = lr.EmployeeId,
                EmployeeName = lr.Employee?.FullName ?? "",
                LeaveTypeName = lr.LeaveType?.Name ?? "",
                StartDate = lr.StartDate,
                EndDate = lr.EndDate,
                TotalDays = CalculateLeaveDays(lr.StartDate, lr.EndDate),
                Reason = lr.Reason,
                Status = lr.Status.ToString(),
                ApprovedByName = lr.Approver?.FullName,
                ApprovedAt = lr.ApprovedAt,
                CreatedAt = lr.CreatedAt
            })
        };
    }

    public async Task<Guid> CreateLeaveRequestAsync(CreateLeaveRequest request, Guid requesterUserId)
    {
        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == requesterUserId);
        if (employee == null) throw new Exception("Employee profile not found");

        var leaveType = await _context.LeaveTypes.FindAsync(request.LeaveTypeId);
        if (leaveType == null) throw new Exception("Invalid Leave Type");

        if (leaveType.EligibleGender.HasValue && leaveType.EligibleGender != employee.Gender)
            throw new Exception($"Jenis cuti ini hanya tersedia untuk karyawan {leaveType.EligibleGender}");

        var reqStartUtc = DateTime.SpecifyKind(request.StartDate, DateTimeKind.Utc);
        var reqEndUtc = DateTime.SpecifyKind(request.EndDate, DateTimeKind.Utc);

        // Validate overlapping
        var overlapping = await _context.LeaveRequests.AnyAsync(lr =>
            lr.EmployeeId == employee.Id &&
            lr.Status != LeaveRequestStatus.Rejected &&
            lr.Status != LeaveRequestStatus.Cancelled &&
            lr.StartDate <= reqEndUtc &&
            lr.EndDate >= reqStartUtc);

        if (overlapping)
            throw new Exception("Leave request overlaps with an existing pending or approved request.");

        int requiredDays = CalculateLeaveDays(request.StartDate, request.EndDate);
        if (requiredDays <= 0)
            throw new Exception("Leave duration must be at least 1 working day.");

        // Check balance
        var balance = await _context.LeaveBalances.FirstOrDefaultAsync(b => 
            b.EmployeeId == employee.Id && 
            b.LeaveTypeId == request.LeaveTypeId && 
            b.Year == DateTime.UtcNow.Year);

        if (balance == null || (balance.TotalDays - balance.UsedDays) < requiredDays)
            throw new Exception($"Insufficient leave balance. You need {requiredDays} days but only have {(balance == null ? 0 : balance.TotalDays - balance.UsedDays)} days remaining.");

        var leaveRequest = new LeaveRequest
        {
            Id = Guid.NewGuid(),
            EmployeeId = employee.Id,
            LeaveTypeId = request.LeaveTypeId,
            StartDate = reqStartUtc,
            EndDate = reqEndUtc,
            Reason = request.Reason,
            Status = LeaveRequestStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _context.LeaveRequests.Add(leaveRequest);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"[NOTIFICATION] New leave request created by {employee.FullName}. Manager ID: {employee.ManagerId}");

        return leaveRequest.Id;
    }

    public async Task ApproveLeaveRequestAsync(Guid id, Guid requesterUserId, bool isRequesterAdmin)
    {
        var leaveReq = await _context.LeaveRequests.Include(lr => lr.Employee).FirstOrDefaultAsync(lr => lr.Id == id);
        if (leaveReq == null) throw new Exception("Leave request not found");

        if (leaveReq.Status != LeaveRequestStatus.Pending)
            throw new Exception("Only pending requests can be approved.");

        var approver = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == requesterUserId);
        
        if (!isRequesterAdmin)
        {
            if (approver == null) throw new Exception("Approver employee profile not found.");
            if (leaveReq.Employee.ManagerId != approver.Id)
                throw new Exception("Forbidden: Only direct manager or admin can approve leave.");
        }

        int daysToDeduct = CalculateLeaveDays(leaveReq.StartDate, leaveReq.EndDate);

        var balance = await _context.LeaveBalances.FirstOrDefaultAsync(b => 
            b.EmployeeId == leaveReq.EmployeeId && 
            b.LeaveTypeId == leaveReq.LeaveTypeId && 
            b.Year == leaveReq.StartDate.Year);

        if (balance == null || (balance.TotalDays - balance.UsedDays) < daysToDeduct)
            throw new Exception("Insufficient leave balance for approval. The employee might have used balances in other requests.");

        balance.UsedDays += daysToDeduct;
        
        leaveReq.Status = LeaveRequestStatus.Approved;
        leaveReq.ApprovedBy = approver?.Id;
        leaveReq.ApprovedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation($"[NOTIFICATION] Leave request {id} APPROVED. Notification sent to employee {leaveReq.EmployeeId}");
    }

    public async Task RejectLeaveRequestAsync(Guid id, RejectLeaveRequest request, Guid requesterUserId, bool isRequesterAdmin)
    {
        var leaveReq = await _context.LeaveRequests.Include(lr => lr.Employee).FirstOrDefaultAsync(lr => lr.Id == id);
        if (leaveReq == null) throw new Exception("Leave request not found");

        if (leaveReq.Status != LeaveRequestStatus.Pending)
            throw new Exception("Only pending requests can be rejected.");

        var approver = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == requesterUserId);
        
        if (!isRequesterAdmin)
        {
            if (approver == null) throw new Exception("Approver employee profile not found.");
            if (leaveReq.Employee.ManagerId != approver.Id)
                throw new Exception("Forbidden: Only direct manager or admin can reject leave.");
        }

        leaveReq.Status = LeaveRequestStatus.Rejected;
        // Prepend reason to existing reason or store it somewhere. Since there's no RejectReason field, let's append it.
        leaveReq.Reason += $" | Rejection Reason: {request.Reason}";
        leaveReq.ApprovedBy = approver?.Id;
        leaveReq.ApprovedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation($"[NOTIFICATION] Leave request {id} REJECTED. Notification sent to employee {leaveReq.EmployeeId}");
    }

    public async Task CancelLeaveRequestAsync(Guid id, Guid requesterUserId)
    {
        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == requesterUserId);
        if (employee == null) throw new Exception("Employee profile not found");

        var leaveReq = await _context.LeaveRequests.FirstOrDefaultAsync(lr => lr.Id == id);
        if (leaveReq == null) throw new Exception("Leave request not found");

        if (leaveReq.EmployeeId != employee.Id)
            throw new Exception("Forbidden: You can only cancel your own leave request.");

        if (leaveReq.Status != LeaveRequestStatus.Pending)
            throw new Exception("Only pending requests can be cancelled.");

        leaveReq.Status = LeaveRequestStatus.Cancelled;
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<LeaveBalanceResponse>> GetLeaveBalancesAsync(Guid employeeId, int year)
    {
        var balances = await _context.LeaveBalances
            .Include(b => b.LeaveType)
            .Where(b => b.EmployeeId == employeeId && b.Year == year)
            .ToListAsync();

        return balances.Select(b => new LeaveBalanceResponse
        {
            LeaveTypeId = b.LeaveTypeId,
            LeaveTypeName = b.LeaveType?.Name ?? "",
            TotalDays = b.TotalDays,
            UsedDays = b.UsedDays
        });
    }

    public async Task InitializeLeaveBalancesAsync(int year)
    {
        var employees = await _context.Employees.Where(e => e.Status == EmployeeStatus.Active).ToListAsync();
        var leaveTypes = await _context.LeaveTypes.ToListAsync();

        var existingBalances = await _context.LeaveBalances.Where(b => b.Year == year).ToListAsync();
        var newBalances = new List<LeaveBalance>();

        foreach (var emp in employees)
        {
            foreach (var type in leaveTypes)
            {
                if (type.EligibleGender != null && type.EligibleGender != emp.Gender) continue;

                if (!existingBalances.Any(b => b.EmployeeId == emp.Id && b.LeaveTypeId == type.Id))
                {
                    int totalDays = type.DefaultDaysPerYear;
                    
                    // Prorate if hired this year
                    if (emp.HireDate.Year == year)
                    {
                        int monthsWorked = 12 - emp.HireDate.Month + 1;
                        totalDays = (int)Math.Round((double)totalDays / 12 * monthsWorked);
                    }
                    else if (emp.HireDate.Year > year)
                    {
                        totalDays = 0; // Not hired yet in this year
                    }

                    newBalances.Add(new LeaveBalance
                    {
                        Id = Guid.NewGuid(),
                        EmployeeId = emp.Id,
                        LeaveTypeId = type.Id,
                        Year = year,
                        TotalDays = totalDays,
                        UsedDays = 0
                    });
                }
            }
        }

        if (newBalances.Any())
        {
            _context.LeaveBalances.AddRange(newBalances);
            await _context.SaveChangesAsync();
        }
    }
}
