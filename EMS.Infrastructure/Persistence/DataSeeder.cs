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
        var expectedPermissions = new[]
        {
            "employee.read", "employee.write", "employee.delete",
            "leave.read", "leave.write", "leave.approve",
            "attendance.read", "attendance.write",
            "department.read", "position.read",
            "dailyreport.read", "dailyreport.write", "dailyreport.review"
        };
        
        var existingPermissions = await context.Permissions.ToListAsync();
        var adminRoleForPerms = await context.Roles.FindAsync(adminRoleId);
        
        foreach (var permName in expectedPermissions)
        {
            var perm = existingPermissions.FirstOrDefault(p => p.Name == permName);
            if (perm == null)
            {
                perm = new Permission { Id = Guid.NewGuid(), Name = permName, CreatedAt = DateTime.UtcNow };
                context.Permissions.Add(perm);
                await context.SaveChangesAsync();
                
                // Assign new permission to Admin by default
                if (adminRoleForPerms != null)
                {
                    context.RolePermissions.Add(new RolePermission { RoleId = adminRoleId, PermissionId = perm.Id });
                }
            }
        }
        await context.SaveChangesAsync();

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
        
        // Pindahkan sisa candidate ke posisi default per departemennya
        var candidates = await context.Candidates.ToListAsync();
        foreach (var cand in candidates)
        {
            var defaultPos = newlyCreatedPositions.FirstOrDefault(p => p.DepartmentId == cand.AppliedDepartmentId) 
                             ?? exec1;
            cand.AppliedPositionId = defaultPos.Id;
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
        
        // 9. Work Shifts Seed & Assignment
        var ohShift = await context.WorkShifts.FirstOrDefaultAsync(s => s.Name == "Office Hour");
        if (ohShift == null)
        {
            ohShift = new WorkShift { Id = Guid.NewGuid(), Name = "Office Hour", StartTime = new TimeSpan(8, 0, 0), EndTime = new TimeSpan(17, 0, 0), IsOvernight = false, ToleranceMinutes = 15, CreatedAt = DateTime.UtcNow };
            context.WorkShifts.Add(ohShift);
        }
        
        var spShift = await context.WorkShifts.FirstOrDefaultAsync(s => s.Name == "Shift Pagi");
        if (spShift == null)
        {
            spShift = new WorkShift { Id = Guid.NewGuid(), Name = "Shift Pagi", StartTime = new TimeSpan(6, 0, 0), EndTime = new TimeSpan(14, 0, 0), IsOvernight = false, ToleranceMinutes = 15, CreatedAt = DateTime.UtcNow };
            context.WorkShifts.Add(spShift);
        }
        
        var ssShift = await context.WorkShifts.FirstOrDefaultAsync(s => s.Name == "Shift Siang");
        if (ssShift == null)
        {
            ssShift = new WorkShift { Id = Guid.NewGuid(), Name = "Shift Siang", StartTime = new TimeSpan(14, 0, 0), EndTime = new TimeSpan(22, 0, 0), IsOvernight = false, ToleranceMinutes = 15, CreatedAt = DateTime.UtcNow };
            context.WorkShifts.Add(ssShift);
        }
        
        var smShift = await context.WorkShifts.FirstOrDefaultAsync(s => s.Name == "Shift Malam");
        if (smShift == null)
        {
            smShift = new WorkShift { Id = Guid.NewGuid(), Name = "Shift Malam", StartTime = new TimeSpan(22, 0, 0), EndTime = new TimeSpan(6, 0, 0), IsOvernight = true, ToleranceMinutes = 15, CreatedAt = DateTime.UtcNow };
            context.WorkShifts.Add(smShift);
        }
        await context.SaveChangesAsync();

        var nonOpDeptNames = new[] { "Human Resources", "Finance & Accounting", "Information Technology", "Sales & Marketing", "Legal & Compliance", "Research & Development" };
        var opsDeptNames = new[] { "Operations", "Customer Service" };
        
        var allEmployees = await context.Employees.Include(e => e.Department).ToListAsync();
        foreach (var emp in allEmployees)
        {
            if (emp.DefaultShiftId == null)
            {
                if (emp.Department != null && opsDeptNames.Contains(emp.Department.Name))
                {
                    // Randomly assign shift for ops/cs
                    emp.DefaultShiftId = (emp.Id.GetHashCode() % 3) switch
                    {
                        0 => spShift.Id,
                        1 => ssShift.Id,
                        _ => smShift.Id
                    };
                }
                else
                {
                    // Default to Office Hour
                    emp.DefaultShiftId = ohShift.Id;
                }
            }
        }
        await context.SaveChangesAsync();

        // 10. Seed Assessment Tests
        var logicQuestionsData = new List<(string text, QuestionCategory category, string[] options, int correctIdx)>
        {
            // Verbal (Sinonim, Antonim, Analogi)
            ("PANDAI >< ...", QuestionCategory.Verbal, new[] { "Pintar", "Bodoh", "Cerdik", "Jenius" }, 1),
            ("BURUNG : TERBANG = IKAN : ...", QuestionCategory.Verbal, new[] { "Insang", "Berenang", "Air", "Sirip" }, 1),
            ("SINONIM dari kata 'MANDIRI' adalah...", QuestionCategory.Verbal, new[] { "Bergantung", "Berdikari", "Bersama", "Bekerja" }, 1),
            ("KENDARAAN : RODA = KAPAL : ...", QuestionCategory.Verbal, new[] { "Laut", "Nahkoda", "Baling-baling", "Jangkar" }, 2),
            ("ANTONIM dari kata 'PROAKTIF' adalah...", QuestionCategory.Verbal, new[] { "Reaktif", "Kreatif", "Pasif", "Aktif" }, 2),
            ("APOTEKER : OBAT = KOKI : ...", QuestionCategory.Verbal, new[] { "Restoran", "Dapur", "Masakan", "Pisau" }, 2),
            
            // Numeric (Deret angka, Aritmatika, Perbandingan)
            ("2, 4, 8, 16, ... angka selanjutnya?", QuestionCategory.Numeric, new[] { "24", "30", "32", "64" }, 2),
            ("Jika harga 5 kg apel adalah Rp 100.000, berapa harga 2 kg apel?", QuestionCategory.Numeric, new[] { "Rp 20.000", "Rp 40.000", "Rp 50.000", "Rp 60.000" }, 1),
            ("10, 15, 21, 28, ... angka selanjutnya?", QuestionCategory.Numeric, new[] { "35", "36", "37", "38" }, 1),
            ("Sebuah kereta melaju dengan kecepatan 60 km/jam. Jarak yang ditempuh dalam 2,5 jam adalah...", QuestionCategory.Numeric, new[] { "120 km", "150 km", "160 km", "180 km" }, 1),
            ("1/2 + 1/4 = ...", QuestionCategory.Numeric, new[] { "3/4", "1/6", "2/4", "2/6" }, 0),
            ("Jika 4 orang pekerja dapat menyelesaikan sebuah pekerjaan dalam 15 hari, berapa hari yang dibutuhkan jika dikerjakan oleh 6 orang pekerja?", QuestionCategory.Numeric, new[] { "8 hari", "10 hari", "12 hari", "14 hari" }, 1),
            
            // Logic (Penalaran, Silogisme, Urutan)
            ("Semua karyawan mendapat cuti. Budi adalah karyawan. Maka...", QuestionCategory.Logic, new[] { "Budi tidak mendapat cuti", "Budi mungkin mendapat cuti", "Budi mendapat cuti", "Budi bukan karyawan" }, 2),
            ("Sebagian mawar berwarna merah. Semua bunga mawar memiliki duri. Maka...", QuestionCategory.Logic, new[] { "Semua mawar berduri merah", "Sebagian mawar berwarna merah dan berduri", "Mawar yang merah tidak berduri", "Semua bunga berduri adalah mawar" }, 1),
            ("Jika turun hujan, maka jalanan basah. Saat ini jalanan tidak basah. Kesimpulannya...", QuestionCategory.Logic, new[] { "Hari ini hujan", "Hari ini tidak hujan", "Jalanan kering karena panas", "Tidak bisa disimpulkan" }, 1),
            ("Andi lebih tinggi dari Budi. Cici lebih pendek dari Budi. Siapa yang paling tinggi?", QuestionCategory.Logic, new[] { "Andi", "Budi", "Cici", "Semua sama tinggi" }, 0),
            ("Tidak ada benda cair yang padat. Es adalah benda padat. Kesimpulannya...", QuestionCategory.Logic, new[] { "Es adalah benda cair", "Es bukan benda cair", "Sebagian es cair", "Air adalah es" }, 1),
            ("Dalam antrian, Dita berada di depan Eko tetapi di belakang Caca. Jika Budi di depan Caca, siapakah yang berada di antrian paling depan?", QuestionCategory.Logic, new[] { "Budi", "Caca", "Dita", "Eko" }, 0)
        };

        var existingLogicTest = await context.Tests.Include(t => t.Questions).ThenInclude(q => q.Options).FirstOrDefaultAsync(t => t.Type == TestType.Logic);
        if (existingLogicTest == null)
        {
            var logicTestId = Guid.NewGuid();
            var logicTest = new Test
            {
                Id = logicTestId,
                Name = "Tes Logika Dasar",
                Description = "Tes logika dan kemampuan analitis dasar (Verbal, Numerik, dan Logika Penalaran).",
                Type = TestType.Logic,
                DurationMinutes = 30,
                IsActive = true
            };

            var logicQuestions = new List<TestQuestion>();
            for (int i = 0; i < logicQuestionsData.Count; i++)
            {
                var qId = Guid.NewGuid();
                var q = new TestQuestion
                {
                    Id = qId,
                    TestId = logicTestId,
                    QuestionText = logicQuestionsData[i].text,
                    QuestionOrder = i + 1,
                    Category = logicQuestionsData[i].category
                };
                
                var options = new List<TestQuestionOption>();
                for (int j = 0; j < 4; j++)
                {
                    options.Add(new TestQuestionOption
                    {
                        Id = Guid.NewGuid(),
                        TestQuestionId = qId,
                        OptionText = logicQuestionsData[i].options[j],
                        OptionOrder = j + 1,
                        IsCorrect = (j == logicQuestionsData[i].correctIdx),
                        TraitCategory = TraitCategory.None
                    });
                }
                q.Options = options;
                logicQuestions.Add(q);
            }
            logicTest.Questions = logicQuestions;
            context.Tests.Add(logicTest);
        }
        else if (existingLogicTest.Questions.Count < logicQuestionsData.Count)
        {
            var orderedExisting = existingLogicTest.Questions.OrderBy(q => q.QuestionOrder).ToList();
            
            for (int i = 0; i < logicQuestionsData.Count; i++)
            {
                var data = logicQuestionsData[i];
                if (i < orderedExisting.Count)
                {
                    // Update existing question in-place
                    var eq = orderedExisting[i];
                    eq.QuestionText = data.text;
                    eq.QuestionOrder = i + 1;
                    eq.Category = data.category;

                    var orderedOptions = eq.Options.OrderBy(o => o.OptionOrder).ToList();
                    for (int j = 0; j < 4; j++)
                    {
                        if (j < orderedOptions.Count)
                        {
                            orderedOptions[j].OptionText = data.options[j];
                            orderedOptions[j].OptionOrder = j + 1;
                            orderedOptions[j].IsCorrect = (j == data.correctIdx);
                        }
                        else
                        {
                            eq.Options.Add(new TestQuestionOption
                            {
                                Id = Guid.NewGuid(),
                                TestQuestionId = eq.Id,
                                OptionText = data.options[j],
                                OptionOrder = j + 1,
                                IsCorrect = (j == data.correctIdx),
                                TraitCategory = TraitCategory.None
                            });
                        }
                    }
                }
                else
                {
                    // Add new question
                    var qId = Guid.NewGuid();
                    var q = new TestQuestion
                    {
                        Id = qId,
                        TestId = existingLogicTest.Id,
                        QuestionText = data.text,
                        QuestionOrder = i + 1,
                        Category = data.category
                    };
                    
                    var options = new List<TestQuestionOption>();
                    for (int j = 0; j < 4; j++)
                    {
                        options.Add(new TestQuestionOption
                        {
                            Id = Guid.NewGuid(),
                            TestQuestionId = qId,
                            OptionText = data.options[j],
                            OptionOrder = j + 1,
                            IsCorrect = (j == data.correctIdx),
                            TraitCategory = TraitCategory.None
                        });
                    }
                    q.Options = options;
                    context.TestQuestions.Add(q);
                }
            }
        }

        if (!await context.Tests.AnyAsync(t => t.Type == TestType.Personality))
        {
            var personalityTestId = Guid.NewGuid();
            var personalityTest = new Test
            {
                Id = personalityTestId,
                Name = "Tes Kepribadian DISC",
                Description = "Tes kepribadian untuk mengetahui profil Dominance, Influence, Steadiness, dan Conscientiousness.",
                Type = TestType.Personality,
                DurationMinutes = 20,
                IsActive = true
            };

            var personalityQuestions = new List<TestQuestion>();
            for (int i = 1; i <= 15; i++)
            {
                var qId = Guid.NewGuid();
                var q = new TestQuestion
                {
                    Id = qId,
                    TestId = personalityTestId,
                    QuestionText = $"Pertanyaan Kepribadian {i}: Manakah pernyataan yang paling menggambarkan diri Anda?",
                    QuestionOrder = i
                };

                q.Options = new List<TestQuestionOption>
                {
                    new TestQuestionOption { Id = Guid.NewGuid(), TestQuestionId = qId, OptionText = "Saya suka memegang kendali dan mengambil keputusan (Dominance)", OptionOrder = 1, TraitCategory = TraitCategory.D },
                    new TestQuestionOption { Id = Guid.NewGuid(), TestQuestionId = qId, OptionText = "Saya suka bergaul dan mempengaruhi orang lain (Influence)", OptionOrder = 2, TraitCategory = TraitCategory.I },
                    new TestQuestionOption { Id = Guid.NewGuid(), TestQuestionId = qId, OptionText = "Saya sabar, pendengar yang baik, dan konsisten (Steadiness)", OptionOrder = 3, TraitCategory = TraitCategory.S },
                    new TestQuestionOption { Id = Guid.NewGuid(), TestQuestionId = qId, OptionText = "Saya teliti, analitis, dan berhati-hati (Conscientiousness)", OptionOrder = 4, TraitCategory = TraitCategory.C }
                };
                personalityQuestions.Add(q);
            }
            personalityTest.Questions = personalityQuestions;
            context.Tests.Add(personalityTest);
        }

        // 7. Seed Sample Job Openings
        if (!await context.JobOpenings.AnyAsync())
        {
            var itDept = await context.Departments.FirstOrDefaultAsync(d => d.Name.Contains("IT") || d.Name.Contains("Technology") || d.Name.Contains("Software"));
            var hrDept = await context.Departments.FirstOrDefaultAsync(d => d.Name.Contains("HR") || d.Name.Contains("Human"));
            var anyDept = itDept ?? hrDept ?? await context.Departments.FirstOrDefaultAsync();

            var devPos = await context.Positions.FirstOrDefaultAsync(p => p.Name.Contains("Developer") || p.Name.Contains("Engineer") || p.Name.Contains("Staff"));
            var hrPos = await context.Positions.FirstOrDefaultAsync(p => p.Name.Contains("HR") || p.Name.Contains("Specialist") || p.Name.Contains("Staff"));
            var anyPos = devPos ?? hrPos ?? await context.Positions.FirstOrDefaultAsync();

            if (anyDept != null && anyPos != null)
            {
                var job1 = new JobOpening
                {
                    Id = Guid.NewGuid(),
                    Title = "Senior Full-Stack Developer",
                    DepartmentId = itDept?.Id ?? anyDept.Id,
                    PositionId = devPos?.Id ?? anyPos.Id,
                    Description = "Kami mencari Full-Stack Developer berpengalaman untuk merancang, membangun, dan memelihara aplikasi web skala enterprise EMS Portal menggunakan React, TypeScript, dan .NET Core Web API.",
                    Requirements = "- Minimal 3 tahun pengalaman dengan React/Vue dan C# / .NET Core\n- Menguasai PostgreSQL / SQL Server dan RESTful API\n- Terbiasa dengan arsitektur Clean Architecture & CQRS\n- Mampu bekerja dalam tim dengan metodologi Agile / Scrum\n- Memiliki komunikasi yang baik dan problem solving yang kuat",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                var job2 = new JobOpening
                {
                    Id = Guid.NewGuid(),
                    Title = "Human Resources Specialist",
                    DepartmentId = hrDept?.Id ?? anyDept.Id,
                    PositionId = hrPos?.Id ?? anyPos.Id,
                    Description = "Bertanggung jawab atas proses rekrutmen end-to-end, onboarding karyawan baru, administrasi data kepegawaian, evaluasi kinerja, dan engagement karyawan.",
                    Requirements = "- Pendidikan minimal S1 Psikologi, Manajemen SDM, atau jurusan terkait\n- Minimal 2 tahun pengalaman di bidang Recruitment & HR Generalist\n- Menguasai administrasi alat tes psikologi dan teknik wawancara berbasis kompetensi (BEI)\n- Memahami regulasi ketenagakerjaan di Indonesia\n- Teliti, komunikatif, dan memiliki empati tinggi",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                var job3 = new JobOpening
                {
                    Id = Guid.NewGuid(),
                    Title = "UI/UX Designer & Product Researcher",
                    DepartmentId = itDept?.Id ?? anyDept.Id,
                    PositionId = devPos?.Id ?? anyPos.Id,
                    Description = "Mendesain antarmuka pengguna yang modern, intuitif, dan responsif untuk seluruh modul ekosistem EMS Portal, serta melakukan riset pengguna.",
                    Requirements = "- Portofolio desain UI/UX web dan mobile app yang kuat\n- Mahir menggunakan Figma, Auto Layout, dan Design System\n- Memahami prinsip-prinsip heuristik UX dan accessibility (WCAG)\n- Mampu berkolaborasi erat dengan Software Engineer",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                context.JobOpenings.AddRange(job1, job2, job3);
            }
        }

        await context.SaveChangesAsync();
    }
}

