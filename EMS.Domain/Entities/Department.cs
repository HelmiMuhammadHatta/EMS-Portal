namespace EMS.Domain.Entities;

public class Department
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}
