using System.ComponentModel.DataAnnotations;

namespace EMS.Application.Settings;

public class RoleDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class CreateRoleDto
{
    [Required]
    public string Name { get; set; } = string.Empty;
}

public class PermissionDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class AssignPermissionsDto
{
    public List<Guid> PermissionIds { get; set; } = new();
}
