using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using EMS.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;

namespace EMS.Application.Employees;

public class EmployeeService : IEmployeeService
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<EmployeeService> _logger;

    public EmployeeService(IApplicationDbContext context, ILogger<EmployeeService> logger)
    {
        _context = context;
        _logger = logger;
    }

    private async Task<bool> IsRequesterAllowedAsync(Guid targetEmployeeId, Guid requesterUserId, bool isRequesterAdmin)
    {
        if (isRequesterAdmin) return true;
        var requesterEmployee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == requesterUserId);
        if (requesterEmployee == null) return false;
        if (requesterEmployee.Id == targetEmployeeId) return true;

        var currentId = targetEmployeeId;
        while (true)
        {
            var target = await _context.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.Id == currentId);
            if (target == null || target.ManagerId == null) return false;
            if (target.ManagerId == requesterEmployee.Id) return true;
            currentId = target.ManagerId.Value;
        }
    }

    private async Task<List<Guid>> GetAllowedEmployeeIdsAsync(Guid requesterUserId, bool isRequesterAdmin)
    {
        if (isRequesterAdmin) return new List<Guid>();

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

    private async Task CheckCircularReferenceAsync(Guid employeeId, Guid? newManagerId)
    {
        if (newManagerId == null) return;
        if (employeeId == newManagerId) throw new Exception("Manager cannot be the employee themselves.");
        
        var currentManagerId = newManagerId;
        while (currentManagerId != null)
        {
            var manager = await _context.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.Id == currentManagerId);
            if (manager == null) break;
            if (manager.Id == employeeId) throw new Exception("Circular reference detected in hierarchy.");
            currentManagerId = manager.ManagerId;
        }
    }

    public async Task<PaginatedResponse<EmployeeResponse>> GetEmployeesAsync(EmployeeListRequest request, Guid requesterUserId, bool isRequesterAdmin)
    {
        var query = _context.Employees
            .Include(e => e.Department)
            .Include(e => e.Position)
            .Include(e => e.Manager)
            .Include(e => e.User)
            .AsQueryable();

        if (!isRequesterAdmin)
        {
            var allowedIds = await GetAllowedEmployeeIdsAsync(requesterUserId, isRequesterAdmin);
            query = query.Where(e => allowedIds.Contains(e.Id));
        }

        if (request.DepartmentId.HasValue)
            query = query.Where(e => e.DepartmentId == request.DepartmentId.Value);
            
        if (request.PositionId.HasValue)
            query = query.Where(e => e.PositionId == request.PositionId.Value);
            
        if (!string.IsNullOrEmpty(request.Status) && Enum.TryParse<EmployeeStatus>(request.Status, true, out var status))
            query = query.Where(e => e.Status == status);

        if (!string.IsNullOrEmpty(request.SearchName))
            query = query.Where(e => e.FullName.ToLower().Contains(request.SearchName.ToLower()));

        query = request.SortBy?.ToLower() switch
        {
            "fullname" => request.SortDirection?.ToLower() == "desc" ? query.OrderByDescending(e => e.FullName) : query.OrderBy(e => e.FullName),
            "hiredate" => request.SortDirection?.ToLower() == "desc" ? query.OrderByDescending(e => e.HireDate) : query.OrderBy(e => e.HireDate),
            _ => query.OrderBy(e => e.FullName)
        };

        var totalCount = await query.CountAsync();
        var items = await query.Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).ToListAsync();

        return new PaginatedResponse<EmployeeResponse>
        {
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize),
            CurrentPage = request.Page,
            Data = items.Select(e => new EmployeeResponse
            {
                Id = e.Id,
                FullName = e.FullName,
                Email = e.User?.Email ?? "",
                Gender = e.Gender?.ToString(),
                DepartmentId = e.DepartmentId,
                DepartmentName = e.Department?.Name ?? "",
                PositionId = e.PositionId,
                PositionName = e.Position?.Name ?? "",
                Status = e.Status.ToString(),
                ManagerName = e.Manager?.FullName,
                ManagerId = e.ManagerId
            })
        };
    }

    public async Task<EmployeeDetailResponse> GetEmployeeByIdAsync(Guid id, Guid requesterUserId, bool isRequesterAdmin)
    {
        if (!await IsRequesterAllowedAsync(id, requesterUserId, isRequesterAdmin))
            throw new Exception("Forbidden");

        var employee = await _context.Employees
            .Include(e => e.Department)
            .Include(e => e.Position)
            .Include(e => e.Manager)
            .Include(e => e.User)
            .Include(e => e.Documents)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (employee == null) throw new Exception("Employee not found");

        return new EmployeeDetailResponse
        {
            Id = employee.Id,
            FullName = employee.FullName,
            DepartmentId = employee.DepartmentId,
            DepartmentName = employee.Department?.Name ?? "",
            PositionId = employee.PositionId,
            PositionName = employee.Position?.Name ?? "",
            Status = employee.Status.ToString(),
            ManagerName = employee.Manager?.FullName,
            ManagerId = employee.ManagerId,
            HireDate = employee.HireDate,
            Email = employee.User?.Email ?? "",
            Documents = employee.Documents.Select(d => new DocumentResponse
            {
                Id = d.Id,
                DocumentType = d.DocumentType.ToString(),
                FilePath = d.FilePath,
                UploadedAt = d.UploadedAt
            })
        };
    }

    public async Task<(Guid Id, string TempPassword)> CreateEmployeeAsync(CreateEmployeeRequest request)
    {
        if (await _context.Users.AnyAsync(u => u.Email == request.Email))
            throw new Exception("Email already exists");

        var staffRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Staff") 
            ?? throw new Exception("Default role not found");

        var tempPassword = Convert.ToBase64String(RandomNumberGenerator.GetBytes(8)).Replace("=", "") + "1aA!";
        
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(tempPassword),
            RoleId = staffRole.Id,
            IsActive = true,
            EmailVerified = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);

        // Validate hierarchy: Manager's Position Level must be < Subordinate's Position Level (lower level number means higher rank)
        if (request.ManagerId != null)
        {
            var manager = await _context.Employees.Include(e => e.Position).FirstOrDefaultAsync(e => e.Id == request.ManagerId);
            if (manager != null)
            {
                var subordinatePosition = await _context.Positions.FindAsync(request.PositionId);
                if (subordinatePosition != null && manager.Position != null)
                {
                    if (manager.Position.Level >= subordinatePosition.Level)
                    {
                        throw new Exception($"Invalid hierarchy: Manager ({manager.Position.Name}, Level {manager.Position.Level}) cannot supervise an employee with a higher or equal rank ({subordinatePosition.Name}, Level {subordinatePosition.Level}). Note: Lower level number indicates higher rank.");
                    }
                }
            }
        }

        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            FullName = request.FullName,
            Gender = string.IsNullOrEmpty(request.Gender) ? null : Enum.Parse<Gender>(request.Gender, true),
            DepartmentId = request.DepartmentId,
            PositionId = request.PositionId,
            ManagerId = request.ManagerId,
            HireDate = request.HireDate.ToUniversalTime(),
            Status = EmployeeStatus.Active,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.Employees.Add(employee);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"[MOCK EMAIL] Account created for {request.Email}. Temporary Password: {tempPassword}");

        return (employee.Id, tempPassword);
    }

    public async Task UpdateEmployeeAsync(Guid id, UpdateEmployeeRequest request, Guid requesterUserId, bool isRequesterAdmin)
    {
        if (!await IsRequesterAllowedAsync(id, requesterUserId, isRequesterAdmin))
            throw new Exception("Forbidden");

        var employee = await _context.Employees.Include(e => e.Position).FirstOrDefaultAsync(e => e.Id == id);
        if (employee == null) throw new Exception("Employee not found");

        await CheckCircularReferenceAsync(id, request.ManagerId);

        // Validate hierarchy: Manager's Position Level must be < Subordinate's Position Level (lower level number means higher rank)
        if (request.ManagerId != null)
        {
            var manager = await _context.Employees.Include(e => e.Position).FirstOrDefaultAsync(e => e.Id == request.ManagerId);
            if (manager != null)
            {
                var subordinatePosition = await _context.Positions.FindAsync(request.PositionId);
                if (subordinatePosition != null && manager.Position != null)
                {
                    if (manager.Position.Level >= subordinatePosition.Level)
                    {
                        throw new Exception($"Invalid hierarchy: Manager ({manager.Position.Name}, Level {manager.Position.Level}) cannot supervise an employee with a higher or equal rank ({subordinatePosition.Name}, Level {subordinatePosition.Level}). Note: Lower level number indicates higher rank.");
                    }
                }
            }
        }

        employee.FullName = request.FullName;
        if (!string.IsNullOrEmpty(request.Gender)) employee.Gender = Enum.Parse<Gender>(request.Gender, true);
        employee.DepartmentId = request.DepartmentId;
        employee.PositionId = request.PositionId;
        employee.ManagerId = request.ManagerId;
        employee.Status = Enum.Parse<EmployeeStatus>(request.Status, true);
        employee.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    public async Task DeleteEmployeeAsync(Guid id, Guid requesterUserId, bool isRequesterAdmin)
    {
        if (!await IsRequesterAllowedAsync(id, requesterUserId, isRequesterAdmin))
            throw new Exception("Forbidden");

        var employee = await _context.Employees.FindAsync(id);
        if (employee == null) throw new Exception("Employee not found");

        employee.IsDeleted = true;
        employee.Status = EmployeeStatus.Terminated;
        employee.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<EmployeeResponse>> GetSubordinatesAsync(Guid managerId, Guid requesterUserId, bool isRequesterAdmin)
    {
        if (!await IsRequesterAllowedAsync(managerId, requesterUserId, isRequesterAdmin))
            throw new Exception("Forbidden");

        var subs = await _context.Employees
            .Include(e => e.Department)
            .Include(e => e.Position)
            .Include(e => e.Manager)
            .Include(e => e.User)
            .Where(e => e.ManagerId == managerId)
            .ToListAsync();

        return subs.Select(e => new EmployeeResponse
        {
            Id = e.Id,
            FullName = e.FullName,
            Email = e.User?.Email ?? "",
            DepartmentId = e.DepartmentId,
            DepartmentName = e.Department?.Name ?? "",
            PositionId = e.PositionId,
            PositionName = e.Position?.Name ?? "",
            Status = e.Status.ToString(),
            ManagerName = e.Manager?.FullName,
            ManagerId = e.ManagerId
        });
    }

    public async Task<object> GetHierarchyAsync(Guid id, Guid requesterUserId, bool isRequesterAdmin)
    {
        if (!await IsRequesterAllowedAsync(id, requesterUserId, isRequesterAdmin))
            throw new Exception("Forbidden");

        var allEmployees = await _context.Employees
            .Include(e => e.Position)
            .Select(e => new { e.Id, e.FullName, Position = e.Position.Name, e.ManagerId })
            .ToListAsync();

        var currentId = id;
        while (true)
        {
            var emp = allEmployees.FirstOrDefault(e => e.Id == currentId);
            if (emp == null || emp.ManagerId == null) break;
            currentId = emp.ManagerId.Value;
        }

        object BuildTree(Guid nodeId)
        {
            var emp = allEmployees.First(e => e.Id == nodeId);
            var children = allEmployees.Where(e => e.ManagerId == nodeId).Select(e => BuildTree(e.Id)).ToList();
            
            return new
            {
                Id = emp.Id,
                Name = emp.FullName,
                Position = emp.Position,
                Subordinates = children
            };
        }

        return BuildTree(currentId);
    }

    public async Task UploadDocumentAsync(Guid employeeId, Stream fileStream, string fileName, string documentType, Guid requesterUserId, bool isRequesterAdmin)
    {
        if (!await IsRequesterAllowedAsync(employeeId, requesterUserId, isRequesterAdmin))
            throw new Exception("Forbidden");

        var employee = await _context.Employees.FindAsync(employeeId);
        if (employee == null) throw new Exception("Employee not found");

        if (fileStream.Length > 5 * 1024 * 1024) throw new Exception("File size exceeds 5MB");

        var ext = Path.GetExtension(fileName).ToLower();
        if (ext != ".pdf" && ext != ".jpg" && ext != ".jpeg" && ext != ".png")
            throw new Exception("Invalid file type. Only PDF, JPG, JPEG, PNG are allowed.");

        var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "employees", employeeId.ToString());
        if (!Directory.Exists(uploadDir)) Directory.CreateDirectory(uploadDir);

        var newFileName = $"{Guid.NewGuid()}{ext}";
        var filePath = Path.Combine(uploadDir, newFileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await fileStream.CopyToAsync(stream);
        }

        var document = new EmployeeDocument
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            DocumentType = Enum.Parse<DocumentType>(documentType, true),
            FilePath = $"/uploads/employees/{employeeId}/{newFileName}",
            UploadedAt = DateTime.UtcNow
        };

        _context.EmployeeDocuments.Add(document);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<DocumentResponse>> GetDocumentsAsync(Guid employeeId, Guid requesterUserId, bool isRequesterAdmin)
    {
        if (!await IsRequesterAllowedAsync(employeeId, requesterUserId, isRequesterAdmin))
            throw new Exception("Forbidden");

        var docs = await _context.EmployeeDocuments.Where(d => d.EmployeeId == employeeId).ToListAsync();
        
        return docs.Select(d => new DocumentResponse
        {
            Id = d.Id,
            DocumentType = d.DocumentType.ToString(),
            FilePath = d.FilePath,
            UploadedAt = d.UploadedAt
        });
    }

    public async Task<(Stream FileStream, string ContentType, string FileName)> DownloadDocumentAsync(Guid employeeId, Guid documentId, Guid requesterUserId, bool isRequesterAdmin)
    {
        if (!await IsRequesterAllowedAsync(employeeId, requesterUserId, isRequesterAdmin))
            throw new Exception("Forbidden");

        var doc = await _context.EmployeeDocuments.FirstOrDefaultAsync(d => d.Id == documentId && d.EmployeeId == employeeId);
        if (doc == null) throw new Exception("Document not found");

        var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var filePath = Path.Combine(uploadDir, doc.FilePath.TrimStart('/'));

        if (!File.Exists(filePath)) throw new Exception("Physical file not found");

        var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        var ext = Path.GetExtension(filePath).ToLower();
        var contentType = ext switch
        {
            ".pdf" => "application/pdf",
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            _ => "application/octet-stream"
        };
        var fileName = $"{doc.DocumentType}{ext}";

        return (stream, contentType, fileName);
    }

    public async Task DeleteDocumentAsync(Guid employeeId, Guid documentId, Guid requesterUserId, bool isRequesterAdmin)
    {
        if (!await IsRequesterAllowedAsync(employeeId, requesterUserId, isRequesterAdmin))
            throw new Exception("Forbidden");

        var doc = await _context.EmployeeDocuments.FirstOrDefaultAsync(d => d.Id == documentId && d.EmployeeId == employeeId);
        if (doc == null) throw new Exception("Document not found");

        var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var filePath = Path.Combine(uploadDir, doc.FilePath.TrimStart('/'));

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        _context.EmployeeDocuments.Remove(doc);
        await _context.SaveChangesAsync();
    }

    public async Task<object> GetAuditLogsAsync(Guid employeeId, int page, int pageSize, Guid requesterUserId, bool isRequesterAdmin)
    {
        if (!await IsRequesterAllowedAsync(employeeId, requesterUserId, isRequesterAdmin))
            throw new Exception("Forbidden");

        var query = _context.AuditLogs
            .Where(a => a.TableName == "Employees" && a.RecordId == employeeId.ToString())
            .OrderByDescending(a => a.ChangedAt);

        var totalCount = await query.CountAsync();
        var logs = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        var changedByUsers = logs.Where(l => l.ChangedBy.HasValue).Select(l => l.ChangedBy.Value).Distinct().ToList();
        var userNames = await _context.Employees
            .Where(e => changedByUsers.Contains(e.UserId))
            .ToDictionaryAsync(e => e.UserId, e => e.FullName);

        return new PaginatedResponse<object>
        {
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
            CurrentPage = page,
            Data = logs.Select(l => new
            {
                Id = l.Id,
                Action = l.Action,
                OldValue = l.OldValue,
                NewValue = l.NewValue,
                ChangedAt = l.ChangedAt,
                ChangedBy = l.ChangedBy,
                ChangedByName = l.ChangedBy.HasValue && userNames.ContainsKey(l.ChangedBy.Value) ? userNames[l.ChangedBy.Value] : "System"
            })
        };
    }

    public async Task ChangePasswordAsync(Guid id, string newPassword, Guid requesterUserId, bool isRequesterAdmin)
    {
        if (!isRequesterAdmin) throw new Exception("Forbidden: Only administrators can change employee passwords.");

        var employee = await _context.Employees.Include(e => e.User).FirstOrDefaultAsync(e => e.Id == id);
        if (employee == null || employee.User == null) throw new Exception("Employee or User not found.");

        employee.User.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        employee.User.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }
}
