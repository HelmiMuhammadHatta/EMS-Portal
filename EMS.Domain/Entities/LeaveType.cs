namespace EMS.Domain.Entities;

public class LeaveType
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public int DefaultDaysPerYear { get; set; }
    public ICollection<LeaveRequest> LeaveRequests { get; set; } = new List<LeaveRequest>();
    public ICollection<LeaveBalance> LeaveBalances { get; set; } = new List<LeaveBalance>();
}
