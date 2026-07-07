namespace EMS.Application.Employees;

public class EmployeeListRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public Guid? DepartmentId { get; set; }
    public Guid? PositionId { get; set; }
    public string? Status { get; set; }
    public string? SearchName { get; set; }
    public string? SortBy { get; set; }
    public string? SortDirection { get; set; }
}

public class PaginatedResponse<T>
{
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public int CurrentPage { get; set; }
    public IEnumerable<T> Data { get; set; } = new List<T>();
}

public class EmployeeResponse
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string DepartmentName { get; set; } = null!;
    public string PositionName { get; set; } = null!;
    public string Status { get; set; } = null!;
    public string? ManagerName { get; set; }
    public Guid? ManagerId { get; set; }
}

public class EmployeeDetailResponse : EmployeeResponse
{
    public DateTime HireDate { get; set; }
    public IEnumerable<DocumentResponse> Documents { get; set; } = new List<DocumentResponse>();
}

public class DocumentResponse
{
    public Guid Id { get; set; }
    public string DocumentType { get; set; } = null!;
    public string FilePath { get; set; } = null!;
    public DateTime UploadedAt { get; set; }
}

public class CreateEmployeeRequest
{
    public string Email { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public Guid DepartmentId { get; set; }
    public Guid PositionId { get; set; }
    public Guid? ManagerId { get; set; }
    public DateTime HireDate { get; set; }
}

public class UpdateEmployeeRequest
{
    public string FullName { get; set; } = null!;
    public Guid DepartmentId { get; set; }
    public Guid PositionId { get; set; }
    public Guid? ManagerId { get; set; }
    public string Status { get; set; } = null!;
}

public class ChangePasswordRequest
{
    public string NewPassword { get; set; } = null!;
}
