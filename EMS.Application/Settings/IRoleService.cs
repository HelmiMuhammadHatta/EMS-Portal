namespace EMS.Application.Settings;

public interface IRoleService
{
    Task<List<RoleDto>> GetRolesAsync();
    Task<RoleDto> CreateRoleAsync(CreateRoleDto request);
    Task<List<PermissionDto>> GetPermissionsAsync();
    Task<List<Guid>> GetRolePermissionsAsync(Guid roleId);
    Task AssignPermissionsAsync(Guid roleId, AssignPermissionsDto request);
}
