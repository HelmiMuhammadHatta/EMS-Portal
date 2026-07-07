using EMS.Domain.Entities;
using EMS.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EMS.Infrastructure.Persistence;

public static class DataSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<EmsDbContext>();
        
        // Ensure database is created and migrations are applied
        await context.Database.MigrateAsync();

        // 1. Roles
        Guid adminRoleId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        Guid managerRoleId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        Guid staffRoleId = Guid.Parse("33333333-3333-3333-3333-333333333333");

        if (!await context.Roles.AnyAsync())
        {
            await context.Roles.AddRangeAsync(
                new Role { Id = adminRoleId, Name = "Admin", CreatedAt = DateTime.UtcNow },
                new Role { Id = managerRoleId, Name = "Manager", CreatedAt = DateTime.UtcNow },
                new Role { Id = staffRoleId, Name = "Staff", CreatedAt = DateTime.UtcNow }
            );
            await context.SaveChangesAsync();
        }

        // 2. Permissions
        if (!await context.Permissions.AnyAsync())
        {
            var permissions = new List<Permission>
            {
                new Permission { Id = Guid.NewGuid(), Name = "employee.read", CreatedAt = DateTime.UtcNow },
                new Permission { Id = Guid.NewGuid(), Name = "employee.write", CreatedAt = DateTime.UtcNow },
                new Permission { Id = Guid.NewGuid(), Name = "employee.delete", CreatedAt = DateTime.UtcNow },
                new Permission { Id = Guid.NewGuid(), Name = "leave.read", CreatedAt = DateTime.UtcNow },
                new Permission { Id = Guid.NewGuid(), Name = "leave.write", CreatedAt = DateTime.UtcNow },
                new Permission { Id = Guid.NewGuid(), Name = "leave.approve", CreatedAt = DateTime.UtcNow },
                new Permission { Id = Guid.NewGuid(), Name = "attendance.read", CreatedAt = DateTime.UtcNow },
                new Permission { Id = Guid.NewGuid(), Name = "attendance.write", CreatedAt = DateTime.UtcNow },
                new Permission { Id = Guid.NewGuid(), Name = "department.read", CreatedAt = DateTime.UtcNow },
                new Permission { Id = Guid.NewGuid(), Name = "position.read", CreatedAt = DateTime.UtcNow }
            };
            
            await context.Permissions.AddRangeAsync(permissions);
            await context.SaveChangesAsync();

            // Assign all permissions to Admin
            var adminRole = await context.Roles.FindAsync(adminRoleId);
            foreach (var perm in permissions)
            {
                context.RolePermissions.Add(new RolePermission { RoleId = adminRoleId, PermissionId = perm.Id });
            }
            await context.SaveChangesAsync();
        }

        // 3. Leave Types (UU Ketenagakerjaan)
        var expectedLeaveTypes = new List<LeaveType>
        {
            new LeaveType { Name = "Cuti Tahunan", DefaultDaysPerYear = 12 },
            new LeaveType { Name = "Cuti Sakit", DefaultDaysPerYear = 365 }, // Unlimited mathematically, depends on doctor's note
            new LeaveType { Name = "Cuti Haid", DefaultDaysPerYear = 24 }, // 2 days per month
            new LeaveType { Name = "Cuti Hamil/Melahirkan", DefaultDaysPerYear = 90 }, // 1.5 months before + 1.5 months after (approx 90 days)
            new LeaveType { Name = "Cuti Penting: Menikah", DefaultDaysPerYear = 3 },
            new LeaveType { Name = "Cuti Penting: Menikahkan Anak", DefaultDaysPerYear = 2 },
            new LeaveType { Name = "Cuti Penting: Khitan/Baptis Anak", DefaultDaysPerYear = 2 },
            new LeaveType { Name = "Cuti Penting: Istri Melahirkan/Keguguran", DefaultDaysPerYear = 2 },
            new LeaveType { Name = "Cuti Penting: Keluarga 1 Rumah Meninggal", DefaultDaysPerYear = 2 },
            new LeaveType { Name = "Cuti Penting: Keluarga Inti Meninggal", DefaultDaysPerYear = 1 },
        };

        foreach (var lt in expectedLeaveTypes)
        {
            if (!await context.LeaveTypes.AnyAsync(l => l.Name == lt.Name))
            {
                lt.Id = Guid.NewGuid();
                context.LeaveTypes.Add(lt);
            }
        }
        await context.SaveChangesAsync();

        // 4. Default Admin User
        if (!await context.Users.AnyAsync(u => u.Email == "admin@ems.local"))
        {
            // Note: In real app, password must be hashed securely using BCrypt/Argon2.
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "admin@ems.local",
                PasswordHash = "Admin123!", 
                RoleId = adminRoleId,
                IsActive = true,
                EmailVerified = true,
                CreatedAt = DateTime.UtcNow
            };
            await context.Users.AddAsync(user);
            await context.SaveChangesAsync();
        }

        var expectedDepts = new[]
        {
            "Human Resources", "Finance & Accounting", "Information Technology",
            "Sales & Marketing", "Operations", "Legal & Compliance",
            "Customer Service", "Research & Development"
        };
        foreach (var d in expectedDepts)
        {
            if (!await context.Departments.AnyAsync(x => x.Name == d))
            {
                await context.Departments.AddAsync(new Department { Id = Guid.NewGuid(), Name = d, CreatedAt = DateTime.UtcNow });
            }
        }
        await context.SaveChangesAsync();

        var expectedPositions = new (string Name, int Level)[]
        {
            ("Chief Executive Officer", 1), ("Chief Operating Officer", 1), ("Chief Financial Officer", 1), ("Chief Technology Officer", 1),
            ("General Manager", 2), ("Department Head / Director", 2),
            ("Senior Manager", 3), ("Manager", 3),
            ("Assistant Manager", 4), ("Team Lead / Supervisor", 4),
            ("Senior Staff / Senior Specialist", 5), ("Staff / Specialist", 5),
            ("Junior Staff", 6), ("Intern", 6)
        };
        foreach (var p in expectedPositions)
        {
            if (!await context.Positions.AnyAsync(x => x.Name == p.Name))
            {
                await context.Positions.AddAsync(new Position { Id = Guid.NewGuid(), Name = p.Name, Level = p.Level, CreatedAt = DateTime.UtcNow });
            }
        }
        await context.SaveChangesAsync();

        // Optional: Update dummy data
        var empHa = await context.Employees.FirstOrDefaultAsync(e => e.FullName == "ha");
        var empHadi = await context.Employees.FirstOrDefaultAsync(e => e.FullName == "Hadi" || e.FullName == "Hahahaha");
        var empHihihihi = await context.Employees.FirstOrDefaultAsync(e => e.FullName == "hihihihihi");
        var empHohohoho = await context.Employees.FirstOrDefaultAsync(e => e.FullName == "hohohoho");

        var allDepts = await context.Departments.ToListAsync();
        var allPos = await context.Positions.ToListAsync();

        if (empHa != null)
        {
            empHa.FullName = "Andi Pratama";
            empHa.DepartmentId = allDepts.First(d => d.Name == "Information Technology").Id;
            empHa.PositionId = allPos.First(p => p.Name == "Chief Technology Officer").Id;
            empHa.ManagerId = null;
        }

        if (empHadi != null)
        {
            empHadi.FullName = "Budi Santoso";
            empHadi.DepartmentId = allDepts.First(d => d.Name == "Information Technology").Id;
            empHadi.PositionId = allPos.First(p => p.Name == "Manager").Id;
            empHadi.ManagerId = empHa?.Id;
        }

        if (empHihihihi != null)
        {
            empHihihihi.FullName = "Citra Dewi";
            empHihihihi.DepartmentId = allDepts.First(d => d.Name == "Human Resources").Id;
            empHihihihi.PositionId = allPos.First(p => p.Name == "Staff / Specialist").Id;
            empHihihihi.ManagerId = empHadi?.Id;
        }

        if (empHohohoho != null)
        {
            empHohohoho.FullName = "Dian Kusuma";
            empHohohoho.DepartmentId = allDepts.First(d => d.Name == "Finance & Accounting").Id;
            empHohohoho.PositionId = allPos.First(p => p.Name == "Staff / Specialist").Id;
            empHohohoho.ManagerId = empHadi?.Id;
        }

        await context.SaveChangesAsync();

        // 6. Default Office Location
        if (!await context.Set<OfficeLocation>().AnyAsync())
        {
            await context.Set<OfficeLocation>().AddAsync(new OfficeLocation { 
                Id = Guid.Parse("77777777-8888-9999-0000-111111111111"), 
                Name = "Jakarta HQ", 
                Latitude = -6.200000, 
                Longitude = 106.816666, 
                RadiusMeters = 100 
            });
            await context.SaveChangesAsync();
        }

        // 7. Initialize Leave Balances for existing employees
        var currentYear = DateTime.UtcNow.Year;
        var activeEmployees = await context.Employees.Where(e => e.Status == EmployeeStatus.Active).ToListAsync();
        var allLeaveTypes = await context.LeaveTypes.ToListAsync();
        
        foreach (var emp in activeEmployees)
        {
            foreach (var type in allLeaveTypes)
            {
                if (!await context.LeaveBalances.AnyAsync(b => b.EmployeeId == emp.Id && b.LeaveTypeId == type.Id && b.Year == currentYear))
                {
                    int totalDays = type.DefaultDaysPerYear;
                    
                    if (emp.HireDate.Year == currentYear && type.Name == "Cuti Tahunan")
                    {
                        int monthsWorked = 12 - emp.HireDate.Month + 1;
                        totalDays = (int)Math.Round((double)totalDays / 12 * monthsWorked);
                    }

                    context.LeaveBalances.Add(new LeaveBalance
                    {
                        Id = Guid.NewGuid(),
                        EmployeeId = emp.Id,
                        LeaveTypeId = type.Id,
                        Year = currentYear,
                        TotalDays = totalDays,
                        UsedDays = 0
                    });
                }
            }
        }
        await context.SaveChangesAsync();
    }
}
