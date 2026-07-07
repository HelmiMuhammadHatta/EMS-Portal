using EMS.Domain.Enums;

namespace EMS.Domain.Entities;

public class Attendance
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;
    public DateTime ClockIn { get; set; }
    public DateTime? ClockOut { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public AttendanceStatus Status { get; set; }
    public string? LocationName { get; set; }
    public string? PhotoPath { get; set; }
    public DateTime CreatedAt { get; set; }
}
