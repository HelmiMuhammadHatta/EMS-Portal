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
    DbSet<WorkShift> WorkShifts { get; }
    DbSet<ShiftSchedule> ShiftSchedules { get; }
    DbSet<ShiftRotationGroup> ShiftRotationGroups { get; }
    DbSet<ShiftRotationPattern> ShiftRotationPatterns { get; }

    // Assessment Module
    DbSet<Test> Tests { get; }
    DbSet<TestQuestion> TestQuestions { get; }
    DbSet<TestQuestionOption> TestQuestionOptions { get; }
    DbSet<TestSession> TestSessions { get; }
    DbSet<TestAnswer> TestAnswers { get; }
    DbSet<TestResult> TestResults { get; }
    DbSet<ProctoringSnapshot> ProctoringSnapshots { get; }

    // Recruitment Module
    DbSet<JobOpening> JobOpenings { get; }
    DbSet<Candidate> Candidates { get; }
    DbSet<CandidateDocument> CandidateDocuments { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
