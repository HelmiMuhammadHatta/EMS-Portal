using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EMS.Application.Settings;

public class RoleService : IRoleService
{
    private readonly IApplicationDbContext _context;

    public RoleService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<RoleDto>> GetRolesAsync()
    {
        return await _context.Roles
            .Select(r => new RoleDto { Id = r.Id, Name = r.Name })
            .ToListAsync();
    }

    public async Task<RoleDto> CreateRoleAsync(CreateRoleDto request)
    {
        var role = new Role
        {
            Id = Guid.NewGuid(),
            Name = request.Name
        };

        _context.Roles.Add(role);
        await _context.SaveChangesAsync();

        return new RoleDto { Id = role.Id, Name = role.Name };
    }

    public async Task<List<PermissionDto>> GetPermissionsAsync()
    {
        return await _context.Permissions
            .Select(p => new PermissionDto { Id = p.Id, Name = p.Name })
            .ToListAsync();
    }

    public async Task<List<Guid>> GetRolePermissionsAsync(Guid roleId)
    {
        return await _context.RolePermissions
            .Where(rp => rp.RoleId == roleId)
            .Select(rp => rp.PermissionId)
            .ToListAsync();
    }

    public async Task AssignPermissionsAsync(Guid roleId, AssignPermissionsDto request)
    {
        var role = await _context.Roles.FindAsync(roleId);
        if (role == null) throw new Exception("Role not found");

        var existing = await _context.RolePermissions.Where(rp => rp.RoleId == roleId).ToListAsync();
        _context.RolePermissions.RemoveRange(existing);

        foreach (var pId in request.PermissionIds)
        {
            _context.RolePermissions.Add(new RolePermission { RoleId = roleId, PermissionId = pId });
        }

        await _context.SaveChangesAsync();
    }
}
