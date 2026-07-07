using EMS.Application.Employees; // For PaginatedResponse
namespace EMS.Application.Leaves;

public class LeaveTypeResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public int DefaultDaysPerYear { get; set; }
}

public class LeaveRequestResponse
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = null!;
    public string LeaveTypeName { get; set; } = null!;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalDays { get; set; }
    public string Reason { get; set; } = null!;
    public string Status { get; set; } = null!;
    public string? ApprovedByName { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class LeaveBalanceResponse
{
    public Guid LeaveTypeId { get; set; }
    public string LeaveTypeName { get; set; } = null!;
    public int TotalDays { get; set; }
    public int UsedDays { get; set; }
    public int RemainingDays => TotalDays - UsedDays;
}

public class CreateLeaveRequest
{
    public Guid LeaveTypeId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Reason { get; set; } = null!;
}

public class LeaveRequestListQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Status { get; set; }
    public Guid? EmployeeId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public class RejectLeaveRequest
{
    public string Reason { get; set; } = null!;
}
