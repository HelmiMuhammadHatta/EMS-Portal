namespace EMS.Domain.Entities;

public class Position
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public int Level { get; set; }
    public Guid? DepartmentId { get; set; }
    public Department? Department { get; set; }
    public DateTime CreatedAt { get; set; }
    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}
