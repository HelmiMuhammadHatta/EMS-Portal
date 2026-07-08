using System.ComponentModel.DataAnnotations;

namespace EMS.Application.Settings;

public class DepartmentDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class CreateDepartmentDto
{
    [Required]
    public string Name { get; set; } = string.Empty;
}

public class PositionDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Level { get; set; }
    public Guid? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
}

public class CreatePositionDto
{
    [Required]
    public string Name { get; set; } = string.Empty;
    public int Level { get; set; }
    public Guid? DepartmentId { get; set; }
}
