using EMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EMS.Application.Interfaces;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<Role> Roles { get; }
    DbSet<Permission> Permissions { get; }
    DbSet<RolePermission> RolePermissions { get; }
    DbSet<Employee> Employees { get; }
    DbSet<Department> Departments { get; }
    DbSet<Position> Positions { get; }
    DbSet<EmployeeDocument> EmployeeDocuments { get; }
    DbSet<LeaveType> LeaveTypes { get; }
    DbSet<LeaveRequest> LeaveRequests { get; }
    DbSet<LeaveBalance> LeaveBalances { get; }
    DbSet<Attendance> Attendances { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<OfficeLocation> OfficeLocations { get; }
    DbSet<DailyReport> DailyReports { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
