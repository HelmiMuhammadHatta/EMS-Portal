using EMS.Domain.Entities;


namespace EMS.Application.Employees;

public interface IEmployeeService
{
    Task<PaginatedResponse<EmployeeResponse>> GetEmployeesAsync(EmployeeListRequest request, Guid requesterUserId, bool isRequesterAdmin);
    Task<EmployeeDetailResponse> GetEmployeeByIdAsync(Guid id, Guid requesterUserId, bool isRequesterAdmin);
    Task<(Guid Id, string TempPassword)> CreateEmployeeAsync(CreateEmployeeRequest request);
    Task UpdateEmployeeAsync(Guid id, UpdateEmployeeRequest request, Guid requesterUserId, bool isRequesterAdmin);
    Task DeleteEmployeeAsync(Guid id, Guid requesterUserId, bool isRequesterAdmin);
    Task<IEnumerable<EmployeeResponse>> GetSubordinatesAsync(Guid managerId, Guid requesterUserId, bool isRequesterAdmin);
    Task<object> GetHierarchyAsync(Guid id, Guid requesterUserId, bool isRequesterAdmin);
    Task UploadDocumentAsync(Guid employeeId, Stream fileStream, string fileName, string documentType, Guid requesterUserId, bool isRequesterAdmin);
    Task<IEnumerable<DocumentResponse>> GetDocumentsAsync(Guid employeeId, Guid requesterUserId, bool isRequesterAdmin);
    Task<IEnumerable<AuditLog>> GetAuditLogsAsync(Guid employeeId, Guid requesterUserId, bool isRequesterAdmin);
    Task ChangePasswordAsync(Guid id, string newPassword, Guid requesterUserId, bool isRequesterAdmin);
}
