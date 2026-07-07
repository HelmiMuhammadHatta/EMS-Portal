using EMS.Application.Employees;

namespace EMS.Application.Attendances;

public class ClockInRequest
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? LocationName { get; set; }
    public string? PhotoBase64 { get; set; }
}

public class ClockOutRequest
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? LocationName { get; set; }
    public string? PhotoBase64 { get; set; }
}

public class AttendanceResponse
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = null!;
    public DateTime ClockIn { get; set; }
    public DateTime? ClockOut { get; set; }
    public string Status { get; set; } = null!;
    public string? LocationName { get; set; }
    public string? PhotoUrl { get; set; }
}

public class AttendanceListQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public Guid? EmployeeId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Status { get; set; }
}

public class AttendanceSummaryResponse
{
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = null!;
    public int Month { get; set; }
    public int Year { get; set; }
    public int TotalPresent { get; set; }
    public int TotalLate { get; set; }
    public int TotalEarlyLeave { get; set; }
    public int TotalAbsent { get; set; } // Can be calculated vs working days
    public double TotalWorkingHours { get; set; }
}
