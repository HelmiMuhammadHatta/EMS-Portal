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

        // 3. Leave Types (5 Standar Baru)
        Console.WriteLine("\n--- MULAI PROSES RESET LEAVE TYPES ---");
        var initialReqCount = await context.LeaveRequests.CountAsync();
        var initialBalCount = await context.LeaveBalances.CountAsync();
        Console.WriteLine($"[LANGKAH 1] Data saat ini: {initialReqCount} LeaveRequests, {initialBalCount} LeaveBalances");

        var expectedLeaveTypes = new List<LeaveType>
        {
            new LeaveType { Name = "Cuti Tahunan", DefaultDaysPerYear = 12, EligibleGender = null },
            new LeaveType { Name = "Sakit", DefaultDaysPerYear = 14, EligibleGender = null },
            new LeaveType { Name = "Izin", DefaultDaysPerYear = 5, EligibleGender = null },
            new LeaveType { Name = "Cuti Melahirkan", DefaultDaysPerYear = 90, EligibleGender = Gender.Female },
            new LeaveType { Name = "Cuti Menikah", DefaultDaysPerYear = 3, EligibleGender = null },
            new LeaveType { Name = "Cuti Haid", DefaultDaysPerYear = 24, EligibleGender = Gender.Female },
            new LeaveType { Name = "Cuti Ayah", DefaultDaysPerYear = 2, EligibleGender = Gender.Male }
        };

        var validLeaveTypeNames = expectedLeaveTypes.Select(lt => lt.Name).ToHashSet();
        var allDbLeaveTypes = await context.LeaveTypes.ToListAsync();
        var validDbLeaveTypes = allDbLeaveTypes.Where(lt => validLeaveTypeNames.Contains(lt.Name)).ToList();

        // 1. Pastikan Tipe Baru Ada dan Properti Update
        foreach (var lt in expectedLeaveTypes)
        {
            var existing = validDbLeaveTypes.FirstOrDefault(v => v.Name == lt.Name);
            if (existing == null)
            {
                lt.Id = Guid.NewGuid();
                context.LeaveTypes.Add(lt);
                validDbLeaveTypes.Add(lt);
            }
            else
            {
                existing.EligibleGender = lt.EligibleGender;
                existing.DefaultDaysPerYear = lt.DefaultDaysPerYear;
            }
        }
        await context.SaveChangesAsync();

        // 2. Migrasi Referensi Lama ke Tipe Baru
        var oldLeaveTypes = allDbLeaveTypes.Where(lt => !validLeaveTypeNames.Contains(lt.Name)).ToList();
        if (oldLeaveTypes.Any())
        {
            var cTahunan = validDbLeaveTypes.First(l => l.Name == "Cuti Tahunan");
            var cSakit = validDbLeaveTypes.First(l => l.Name == "Sakit");
            var cIzin = validDbLeaveTypes.First(l => l.Name == "Izin");
            var cMelahirkan = validDbLeaveTypes.First(l => l.Name == "Cuti Melahirkan");
            var cMenikah = validDbLeaveTypes.First(l => l.Name == "Cuti Menikah");

            var leaveRequests = await context.LeaveRequests.ToListAsync();
            var leaveBalances = await context.LeaveBalances.ToListAsync();

            Console.WriteLine($"[LANGKAH 2] Mapping {leaveRequests.Count} LeaveRequests ke tipe standar baru...");
            foreach(var lt in oldLeaveTypes)
            {
                Guid targetId = cIzin.Id; // Default fallback
                if (lt.Name.Contains("Sakit", StringComparison.OrdinalIgnoreCase)) targetId = cSakit.Id;
                else if (lt.Name.Contains("Hamil", StringComparison.OrdinalIgnoreCase) || lt.Name.Contains("Melahirkan", StringComparison.OrdinalIgnoreCase)) targetId = cMelahirkan.Id;
                else if (lt.Name.Contains("Menikah", StringComparison.OrdinalIgnoreCase)) targetId = cMenikah.Id;
                else if (lt.Name.Contains("Tahunan", StringComparison.OrdinalIgnoreCase)) targetId = cTahunan.Id;

                foreach(var req in leaveRequests.Where(r => r.LeaveTypeId == lt.Id)) req.LeaveTypeId = targetId;
                foreach(var bal in leaveBalances.Where(b => b.LeaveTypeId == lt.Id)) bal.LeaveTypeId = targetId;
            }

            Console.WriteLine($"[LANGKAH 3] Membersihkan duplikat LeaveBalances sebelum disave...");
            var groupedBalances = leaveBalances
                .GroupBy(b => new { b.EmployeeId, b.LeaveTypeId, b.Year })
                .Where(g => g.Count() > 1)
                .ToList();
            
            foreach(var group in groupedBalances)
            {
                var keep = group.First();
                var duplicates = group.Skip(1).ToList();
                foreach(var dup in duplicates) {
                    keep.UsedDays += dup.UsedDays;
                    context.LeaveBalances.Remove(dup);
                }
            }
            // Save after re-mapping and cleaning duplicates
            await context.SaveChangesAsync();
            
            Console.WriteLine($"[LANGKAH 4] Menghapus {oldLeaveTypes.Count} LeaveType lama yang tidak valid...");
            context.LeaveTypes.RemoveRange(oldLeaveTypes);
            await context.SaveChangesAsync();
        }

        Console.WriteLine("\n[LANGKAH 5 & 6] Verifikasi hasil akhir LeaveType di Database:");
        var finalTypes = await context.LeaveTypes.ToListAsync();
        foreach (var t in finalTypes) {
            Console.WriteLine($"- ID: {t.Id}, Name: '{t.Name}', Quota: {t.DefaultDaysPerYear}");
        }
        Console.WriteLine($"TOTAL LeaveType saat ini: {finalTypes.Count} (Harus 7)");
        Console.WriteLine("--- SELESAI PROSES RESET LEAVE TYPES ---\n");

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

        // Migrate "IT Department" to "Information Technology" if exists
        var oldItDept = await context.Departments.FirstOrDefaultAsync(d => d.Name == "IT Department");
        if (oldItDept != null)
        {
            var newItDept = await context.Departments.FirstAsync(d => d.Name == "Information Technology");
            var employeesInOldIt = await context.Employees.Where(e => e.DepartmentId == oldItDept.Id).ToListAsync();
            foreach (var emp in employeesInOldIt) emp.DepartmentId = newItDept.Id;
            context.Departments.Remove(oldItDept);
            await context.SaveChangesAsync();
        }

        // LANGKAH 1 & 3 — SEED POSISI BARU DULU SEBELUM MENGHAPUS YANG LAMA
        // Karena Employee.PositionId bersifat non-nullable (Guid), kita harus insert posisi baru dulu, 
        // menggeser semua karyawan ke posisi baru, baru kita bisa menghapus posisi lama tanpa melanggar FK Constraint.
        var allOldPositions = await context.Positions.ToListAsync();
        var departments = await context.Departments.ToListAsync();
        
        Console.WriteLine("\n[LANGKAH 2] Daftar Department dari Database:");
        foreach(var d in departments) {
            Console.WriteLine($"- ID: {d.Id}, Name: {d.Name}");
        }

        var positionDictionary = new Dictionary<string, List<(string Name, int Level)>>(StringComparer.OrdinalIgnoreCase)
        {
            { "Information Technology", new List<(string, int)> { ("IT Director", 2), ("IT Manager", 3), ("Software Engineer", 5), ("QA Engineer", 5), ("IT Support", 5), ("IT Intern", 6) } },
            { "Human Resources", new List<(string, int)> { ("HR Director", 2), ("HR Manager", 3), ("HR Staff", 5), ("HR Intern", 6) } },
            { "Finance & Accounting", new List<(string, int)> { ("Finance Director", 2), ("Finance Manager", 3), ("Financial Analyst", 5), ("Accounting Staff", 5), ("Finance Intern", 6) } },
            { "Sales & Marketing", new List<(string, int)> { ("Sales Director", 2), ("Sales Manager", 3), ("Marketing Manager", 3), ("Sales Executive", 5), ("Marketing Staff", 5) } },
            { "Operations", new List<(string, int)> { ("Operations Director", 2), ("Operations Manager", 3), ("Operations Staff", 5) } },
            { "Legal & Compliance", new List<(string, int)> { ("Legal Director", 2), ("Legal Manager", 3), ("Legal Staff", 5) } },
            { "Customer Service", new List<(string, int)> { ("Customer Service Manager", 3), ("Customer Service Staff", 5) } },
            { "Research & Development", new List<(string, int)> { ("R&D Director", 2), ("R&D Manager", 3), ("Research Staff", 5) } }
        };

        var newlyCreatedPositions = new List<Position>();

        Console.WriteLine("\n[LANGKAH 3] Proses Seeding Posisi per Department:");
        foreach(var dept in departments) {
            if (positionDictionary.TryGetValue(dept.Name, out var positionsToInsert)) {
                int count = 0;
                foreach(var pos in positionsToInsert) {
                    var newPos = new Position { Id = Guid.NewGuid(), Name = pos.Name, Level = pos.Level, DepartmentId = dept.Id, CreatedAt = DateTime.UtcNow };
                    context.Positions.Add(newPos);
                    newlyCreatedPositions.Add(newPos);
                    count++;
                }
                Console.WriteLine($"Department {dept.Name} -> berhasil insert {count} posisi");
            } else {
                Console.WriteLine($"WARNING: Department '{dept.Name}' tidak ketemu pasangannya di dictionary!");
            }
        }
        
        var exec1 = new Position { Id = Guid.NewGuid(), Name = "Chief Executive Officer", Level = 1, DepartmentId = null, CreatedAt = DateTime.UtcNow };
        var exec2 = new Position { Id = Guid.NewGuid(), Name = "Chief Operating Officer", Level = 1, DepartmentId = null, CreatedAt = DateTime.UtcNow };
        var exec3 = new Position { Id = Guid.NewGuid(), Name = "Chief Financial Officer", Level = 1, DepartmentId = null, CreatedAt = DateTime.UtcNow };
        var exec4 = new Position { Id = Guid.NewGuid(), Name = "Chief Technology Officer", Level = 1, DepartmentId = null, CreatedAt = DateTime.UtcNow };
        context.Positions.AddRange(exec1, exec2, exec3, exec4);
        newlyCreatedPositions.AddRange(new[] { exec1, exec2, exec3, exec4 });
        
        await context.SaveChangesAsync();

        var deptDict = departments.ToDictionary(d => d.Name, d => d.Id);
        var employees = await context.Employees.ToListAsync();
        
        // Pindahkan HAPUS POSISI ke sini dengan mengubah posisi employee ke posisi yang valid.
        var empHa = employees.FirstOrDefault(e => e.FullName == "Andi Pratama");
        var empHadi = employees.FirstOrDefault(e => e.FullName == "Budi Santoso");
        var empHihihihi = employees.FirstOrDefault(e => e.FullName == "Citra Dewi");
        var empHohohoho = employees.FirstOrDefault(e => e.FullName == "Dian Kusuma");

        if (empHa != null && deptDict.ContainsKey("Information Technology"))
        {
            empHa.DepartmentId = deptDict["Information Technology"];
            var cto = newlyCreatedPositions.FirstOrDefault(p => p.Name == "Chief Technology Officer");
            if (cto != null) empHa.PositionId = cto.Id;
            empHa.ManagerId = null;
        }

        if (empHadi != null && deptDict.ContainsKey("Information Technology"))
        {
            empHadi.DepartmentId = deptDict["Information Technology"];
            var itMgr = newlyCreatedPositions.FirstOrDefault(p => p.Name == "IT Manager");
            if (itMgr != null) empHadi.PositionId = itMgr.Id;
            empHadi.ManagerId = empHa?.Id;
        }

        if (empHihihihi != null && deptDict.ContainsKey("Human Resources"))
        {
            empHihihihi.DepartmentId = deptDict["Human Resources"];
            var hrStaff = newlyCreatedPositions.FirstOrDefault(p => p.Name == "HR Staff");
            if (hrStaff != null) empHihihihi.PositionId = hrStaff.Id;
            empHihihihi.ManagerId = empHadi?.Id;
        }

        if (empHohohoho != null && deptDict.ContainsKey("Finance & Accounting"))
        {
            empHohohoho.DepartmentId = deptDict["Finance & Accounting"];
            var finAnalyst = newlyCreatedPositions.FirstOrDefault(p => p.Name == "Financial Analyst");
            if (finAnalyst != null) empHohohoho.PositionId = finAnalyst.Id;
            empHohohoho.ManagerId = empHadi?.Id;
        }

        // Pindahkan sisa employee ke posisi default per departemennya
        foreach (var emp in employees)
        {
            if (emp.Id != empHa?.Id && emp.Id != empHadi?.Id && emp.Id != empHihihihi?.Id && emp.Id != empHohohoho?.Id)
            {
                var defaultPos = newlyCreatedPositions.FirstOrDefault(p => p.DepartmentId == emp.DepartmentId) 
                                 ?? exec1;
                emp.PositionId = defaultPos.Id;
            }
        }
        await context.SaveChangesAsync();

        // SEKARANG kita bisa hapus posisi lama karena semua foreign key sudah diganti ke posisi baru
        context.Positions.RemoveRange(allOldPositions);
        await context.SaveChangesAsync();
        
        var deletedPosCount = allOldPositions.Count;
        Console.WriteLine($"\n[LANGKAH 1] Berhasil menghapus {deletedPosCount} posisi lama setelah relokasi karyawan.");

        // LANGKAH 4 — VERIFIKASI DATA DI DATABASE
        Console.WriteLine("\n[LANGKAH 4] Hasil Verifikasi Data di Database:");
        var posWithDepts = await context.Positions
            .Include(p => p.Department)
            .OrderBy(p => p.Department != null ? p.Department.Name : "zzz")
            .ThenBy(p => p.Level)
            .ToListAsync();
            
        foreach(var p in posWithDepts) {
            Console.WriteLine($"{p.Department?.Name ?? "NULL (Executive)"} | {p.Name} | {p.Level}");
        }
        
        Console.WriteLine("\nRekap Jumlah Posisi per Department:");
        var deptStats = await context.Departments
            .Select(d => new { d.Name, Count = context.Positions.Count(p => p.DepartmentId == d.Id) })
            .ToListAsync();
            
        foreach(var stat in deptStats) {
            Console.WriteLine($"{stat.Name} -> {stat.Count} posisi");
            if (stat.Count == 0) {
                Console.WriteLine($"WARNING: {stat.Name} HAS 0 POSITIONS!");
            }
        }
        Console.WriteLine("-------------------------------------------\n");

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

        // 7. Initialize Leave Balances for existing employees and set Genders
        var currentYear = DateTime.UtcNow.Year;
        var activeEmployees = await context.Employees.Where(e => e.Status == EmployeeStatus.Active).ToListAsync();
        var finalLeaveTypes = await context.LeaveTypes.ToListAsync();
        
        foreach (var emp in activeEmployees)
        {
            // Assign dummy gender
            if (!emp.Gender.HasValue) 
            {
                if (emp.FullName.Contains("Citra") || emp.FullName.Contains("Dian") || emp.FullName.Contains("Dewi") || emp.FullName.Contains("Ayu") || emp.FullName.Contains("Putri") || emp.FullName.Contains("Siti"))
                    emp.Gender = Gender.Female;
                else
                    emp.Gender = Gender.Male;
            }

            foreach (var type in finalLeaveTypes)
            {
                bool isEligible = type.EligibleGender == null || type.EligibleGender == emp.Gender;

                var existingBalance = await context.LeaveBalances.FirstOrDefaultAsync(b => b.EmployeeId == emp.Id && b.LeaveTypeId == type.Id && b.Year == currentYear);
                
                if (!isEligible) 
                {
                    if (existingBalance != null) context.LeaveBalances.Remove(existingBalance);
                    continue;
                }

                if (existingBalance == null)
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

        // TEST SCENARIO: Update Employee 'ha' (Andi Pratama) balance to Total 12, Used 10 
        // to verify the RED progress bar color on the frontend.
        var haEmp = activeEmployees.FirstOrDefault(e => e.FullName == "Andi Pratama");
        if (haEmp != null) {
            var tahType = finalLeaveTypes.FirstOrDefault(t => t.Name == "Cuti Tahunan");
            if (tahType != null) {
                var haBal = await context.LeaveBalances.FirstOrDefaultAsync(b => b.EmployeeId == haEmp.Id && b.LeaveTypeId == tahType.Id && b.Year == currentYear);
                if (haBal != null) {
                    haBal.TotalDays = 12;
                    haBal.UsedDays = 10;
                }
            }
        }

        Console.WriteLine($"\n[INFO] Berhasil mereset Leave Types menjadi {finalLeaveTypes.Count} tipe standar, dan menormalkan Leave Balances.");
        await context.SaveChangesAsync();

        // 8. Add dummy attendance for today for testing Dashboard chart
        var today = DateTime.UtcNow.Date;
        var hasAttendancesToday = await context.Attendances.AnyAsync(a => a.ClockIn >= today && a.ClockIn < today.AddDays(1));
        if (!hasAttendancesToday && activeEmployees.Count >= 3)
        {
            var emp1 = activeEmployees[0];
            var emp2 = activeEmployees[1];
            var emp3 = activeEmployees[2];

            context.Attendances.AddRange(
                new Attendance
                {
                    Id = Guid.NewGuid(),
                    EmployeeId = emp1.Id,
                    ClockIn = today.AddHours(8).AddMinutes(15), // 08:15 AM
                    Status = AttendanceStatus.OnTime,
                    CreatedAt = DateTime.UtcNow
                },
                new Attendance
                {
                    Id = Guid.NewGuid(),
                    EmployeeId = emp2.Id,
                    ClockIn = today.AddHours(9).AddMinutes(5), // 09:05 AM
                    Status = AttendanceStatus.Late,
                    CreatedAt = DateTime.UtcNow
                },
                new Attendance
                {
                    Id = Guid.NewGuid(),
                    EmployeeId = emp3.Id,
                    ClockIn = today.AddHours(8).AddMinutes(45), // 08:45 AM
                    Status = AttendanceStatus.OnTime,
                    CreatedAt = DateTime.UtcNow
                }
            );
            await context.SaveChangesAsync();
            Console.WriteLine("\n[INFO] Berhasil menambahkan 3 data attendance dummy untuk hari ini.");
        }
    }
}
