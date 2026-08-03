using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using EMS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EMS.Infrastructure.Persistence;

public class EmsDbContext : DbContext, IApplicationDbContext
{
    private readonly ICurrentUserService _currentUserService;

    public EmsDbContext(DbContextOptions<EmsDbContext> options, ICurrentUserService currentUserService) : base(options) 
    {
        _currentUserService = currentUserService;
    }
    
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Position> Positions => Set<Position>();
    public DbSet<EmployeeDocument> EmployeeDocuments => Set<EmployeeDocument>();
    public DbSet<LeaveType> LeaveTypes => Set<LeaveType>();
    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();
    public DbSet<LeaveBalance> LeaveBalances => Set<LeaveBalance>();
    public DbSet<Attendance> Attendances => Set<Attendance>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<OfficeLocation> OfficeLocations => Set<OfficeLocation>();
    public DbSet<DailyReport> DailyReports => Set<DailyReport>();
    public DbSet<WorkShift> WorkShifts => Set<WorkShift>();
    public DbSet<ShiftSchedule> ShiftSchedules => Set<ShiftSchedule>();
    public DbSet<ShiftRotationGroup> ShiftRotationGroups => Set<ShiftRotationGroup>();
    public DbSet<ShiftRotationPattern> ShiftRotationPatterns => Set<ShiftRotationPattern>();
    
    // Assessment Module
    public DbSet<Test> Tests => Set<Test>();
    public DbSet<TestQuestion> TestQuestions => Set<TestQuestion>();
    public DbSet<TestQuestionOption> TestQuestionOptions => Set<TestQuestionOption>();
    public DbSet<TestSession> TestSessions => Set<TestSession>();
    public DbSet<TestAnswer> TestAnswers => Set<TestAnswer>();
    public DbSet<TestResult> TestResults => Set<TestResult>();
    public DbSet<ProctoringSnapshot> ProctoringSnapshots => Set<ProctoringSnapshot>();

    // Recruitment Module
    public DbSet<JobOpening> JobOpenings => Set<JobOpening>();
    public DbSet<Candidate> Candidates => Set<Candidate>();
    public DbSet<CandidateDocument> CandidateDocuments => Set<CandidateDocument>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.HasPostgresExtension("pgcrypto");

        // 1. Configure default Guid for all entities
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var idProperty = entityType.FindProperty("Id");
            if (idProperty != null && idProperty.ClrType == typeof(Guid))
            {
                idProperty.SetDefaultValueSql("gen_random_uuid()");
            }
        }

        // 2. RolePermission (Many-to-Many)
        modelBuilder.Entity<RolePermission>()
            .HasKey(rp => new { rp.RoleId, rp.PermissionId });
            
        modelBuilder.Entity<RolePermission>()
            .HasOne(rp => rp.Role)
            .WithMany(r => r.RolePermissions)
            .HasForeignKey(rp => rp.RoleId);
            
        modelBuilder.Entity<RolePermission>()
            .HasOne(rp => rp.Permission)
            .WithMany(p => p.RolePermissions)
            .HasForeignKey(rp => rp.PermissionId);

        // Position -> Department (Nullable)
        modelBuilder.Entity<Position>()
            .HasOne(p => p.Department)
            .WithMany()
            .HasForeignKey(p => p.DepartmentId)
            .OnDelete(DeleteBehavior.SetNull);

        // 3. User -> Employee (One-to-One)
        modelBuilder.Entity<Employee>()
            .HasOne(e => e.User)
            .WithOne(u => u.Employee)
            .HasForeignKey<Employee>(e => e.UserId);

        // 4. Employee -> Manager (Self-Reference One-to-Many)
        modelBuilder.Entity<Employee>()
            .HasOne(e => e.Manager)
            .WithMany(m => m.Subordinates)
            .HasForeignKey(e => e.ManagerId)
            .OnDelete(DeleteBehavior.Restrict);

        // 5. LeaveRequest Approver (Employee -> LeaveRequest)
        modelBuilder.Entity<LeaveRequest>()
            .HasOne(lr => lr.Approver)
            .WithMany(e => e.ApprovedLeaveRequests)
            .HasForeignKey(lr => lr.ApprovedBy)
            .OnDelete(DeleteBehavior.Restrict);
            
        modelBuilder.Entity<LeaveRequest>()
            .HasOne(lr => lr.Employee)
            .WithMany(e => e.LeaveRequests)
            .HasForeignKey(lr => lr.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        // 6. Global Query Filter for Soft Delete
        modelBuilder.Entity<Employee>().HasQueryFilter(e => !e.IsDeleted);

        // 7. Indexes
        modelBuilder.Entity<Employee>().HasIndex(e => e.DepartmentId);
        modelBuilder.Entity<LeaveRequest>().HasIndex(lr => lr.EmployeeId);
        modelBuilder.Entity<Attendance>().HasIndex(a => a.EmployeeId);
        modelBuilder.Entity<Attendance>().HasIndex(a => a.ClockIn);
        
        // 8. DailyReport
        modelBuilder.Entity<DailyReport>()
            .HasIndex(dr => new { dr.EmployeeId, dr.ReportDate })
            .IsUnique();
            
        modelBuilder.Entity<DailyReport>()
            .HasOne(dr => dr.Reviewer)
            .WithMany()
            .HasForeignKey(dr => dr.ReviewedBy)
            .OnDelete(DeleteBehavior.SetNull);

        // 9. Employee -> DefaultShift (Nullable)
        modelBuilder.Entity<Employee>()
            .HasOne(e => e.DefaultShift)
            .WithMany()
            .HasForeignKey(e => e.DefaultShiftId)
            .OnDelete(DeleteBehavior.SetNull);
            
        // 10. ShiftSchedule relations
        modelBuilder.Entity<ShiftSchedule>()
            .HasOne(ss => ss.Employee)
            .WithMany()
            .HasForeignKey(ss => ss.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);
            
        modelBuilder.Entity<ShiftSchedule>()
            .HasOne(ss => ss.WorkShift)
            .WithMany()
            .HasForeignKey(ss => ss.WorkShiftId)
            .OnDelete(DeleteBehavior.Restrict);
            
        modelBuilder.Entity<ShiftSchedule>()
            .HasIndex(ss => new { ss.EmployeeId, ss.Date })
            .IsUnique();
            
        // 11. Shift Rotation logic
        modelBuilder.Entity<Employee>()
            .HasOne(e => e.RotationGroup)
            .WithMany(rg => rg.Employees)
            .HasForeignKey(e => e.RotationGroupId)
            .OnDelete(DeleteBehavior.SetNull);
            
        modelBuilder.Entity<ShiftRotationPattern>()
            .HasOne(srp => srp.RotationGroup)
            .WithMany(rg => rg.Patterns)
            .HasForeignKey(srp => srp.RotationGroupId)
            .OnDelete(DeleteBehavior.Cascade);
            
        modelBuilder.Entity<ShiftRotationPattern>()
            .HasOne(srp => srp.WorkShift)
            .WithMany()
            .HasForeignKey(srp => srp.WorkShiftId)
            .OnDelete(DeleteBehavior.Restrict);
            
        modelBuilder.Entity<ShiftRotationPattern>()
            .HasIndex(srp => new { srp.RotationGroupId, srp.CycleWeekNumber })
            .IsUnique();

        // 12. Assessment Module configurations
        modelBuilder.Entity<TestQuestion>()
            .HasOne(tq => tq.Test)
            .WithMany(t => t.Questions)
            .HasForeignKey(tq => tq.TestId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TestQuestionOption>()
            .HasOne(tqo => tqo.TestQuestion)
            .WithMany(tq => tq.Options)
            .HasForeignKey(tqo => tqo.TestQuestionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TestSession>()
            .HasOne(ts => ts.Test)
            .WithMany()
            .HasForeignKey(ts => ts.TestId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TestAnswer>()
            .HasOne(ta => ta.TestSession)
            .WithMany(ts => ts.Answers)
            .HasForeignKey(ta => ta.TestSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TestAnswer>()
            .HasOne(ta => ta.TestQuestion)
            .WithMany()
            .HasForeignKey(ta => ta.TestQuestionId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TestAnswer>()
            .HasOne(ta => ta.SelectedOption)
            .WithMany()
            .HasForeignKey(ta => ta.SelectedOptionId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TestResult>()
            .HasOne(tr => tr.TestSession)
            .WithOne(ts => ts.Result)
            .HasForeignKey<TestResult>(tr => tr.TestSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ProctoringSnapshot>()
            .HasOne(p => p.TestSession)
            .WithMany(ts => ts.ProctoringSnapshots)
            .HasForeignKey(p => p.TestSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        // 13. Recruitment Module configurations
        modelBuilder.Entity<JobOpening>()
            .HasOne(j => j.Department)
            .WithMany()
            .HasForeignKey(j => j.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<JobOpening>()
            .HasOne(j => j.Position)
            .WithMany()
            .HasForeignKey(j => j.PositionId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Candidate>()
            .HasOne(c => c.JobOpening)
            .WithMany(j => j.Candidates)
            .HasForeignKey(c => c.JobOpeningId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Candidate>()
            .HasOne(c => c.AppliedDepartment)
            .WithMany()
            .HasForeignKey(c => c.AppliedDepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Candidate>()
            .HasOne(c => c.AppliedPosition)
            .WithMany()
            .HasForeignKey(c => c.AppliedPositionId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CandidateDocument>()
            .HasOne(cd => cd.Candidate)
            .WithMany(c => c.Documents)
            .HasForeignKey(cd => cd.CandidateId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Employee>()
            .HasOne(e => e.Candidate)
            .WithOne()
            .HasForeignKey<Employee>(e => e.CandidateId)
            .OnDelete(DeleteBehavior.SetNull);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries<Employee>().Where(e => e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted).ToList();
        
        var auditLogs = new List<AuditLog>();
        var userId = _currentUserService.UserId;

        foreach (var entry in entries)
        {
            var action = entry.State switch
            {
                EntityState.Added => AuditAction.Create,
                EntityState.Modified => AuditAction.Update,
                EntityState.Deleted => AuditAction.Delete,
                _ => AuditAction.Update
            };

            var auditLog = new AuditLog
            {
                Id = Guid.NewGuid(),
                TableName = "Employees",
                RecordId = entry.Property("Id").CurrentValue?.ToString() ?? "",
                Action = action,
                ChangedBy = userId,
                ChangedAt = DateTime.UtcNow
            };

            var oldValues = new Dictionary<string, object?>();
            var newValues = new Dictionary<string, object?>();

            if (entry.State == EntityState.Added)
            {
                foreach (var prop in entry.CurrentValues.Properties)
                {
                    newValues[prop.Name] = entry.CurrentValues[prop];
                }
                auditLog.NewValue = System.Text.Json.JsonSerializer.Serialize(newValues);
            }
            else if (entry.State == EntityState.Deleted)
            {
                foreach (var prop in entry.OriginalValues.Properties)
                {
                    oldValues[prop.Name] = entry.OriginalValues[prop];
                }
                auditLog.OldValue = System.Text.Json.JsonSerializer.Serialize(oldValues);
            }
            else if (entry.State == EntityState.Modified)
            {
                foreach (var prop in entry.OriginalValues.Properties)
                {
                    // Skip tracking navigation properties or internal EF fields if any (though these are scalar properties)
                    var originalValue = entry.OriginalValues[prop];
                    var currentValue = entry.CurrentValues[prop];
                    
                    if (!Equals(originalValue, currentValue))
                    {
                        oldValues[prop.Name] = originalValue;
                        newValues[prop.Name] = currentValue;
                    }
                }
                
                if (!oldValues.Any())
                {
                    continue; // Skip creating audit log if nothing actually changed
                }
                
                auditLog.OldValue = System.Text.Json.JsonSerializer.Serialize(oldValues);
                auditLog.NewValue = System.Text.Json.JsonSerializer.Serialize(newValues);
            }

            auditLogs.Add(auditLog);
        }

        if (auditLogs.Any())
        {
            AuditLogs.AddRange(auditLogs);
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
