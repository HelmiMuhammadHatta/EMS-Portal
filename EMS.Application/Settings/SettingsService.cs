using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EMS.Application.Settings;

public class DepartmentService : IDepartmentService
{
    private readonly IApplicationDbContext _context;

    public DepartmentService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<DepartmentDto>> GetAllAsync()
    {
        return await _context.Departments
            .Select(d => new DepartmentDto { Id = d.Id, Name = d.Name })
            .ToListAsync();
    }

    public async Task<DepartmentDto> CreateAsync(CreateDepartmentDto request)
    {
        var dept = new Department
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            CreatedAt = DateTime.UtcNow
        };

        _context.Departments.Add(dept);
        await _context.SaveChangesAsync();

        return new DepartmentDto { Id = dept.Id, Name = dept.Name };
    }

    public async Task DeleteAsync(Guid id)
    {
        var dept = await _context.Departments.FindAsync(id);
        if (dept != null)
        {
            _context.Departments.Remove(dept);
            await _context.SaveChangesAsync();
        }
    }
}

public class PositionService : IPositionService
{
    private readonly IApplicationDbContext _context;

    public PositionService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<PositionDto>> GetAllAsync()
    {
        return await _context.Positions
            .Select(p => new PositionDto { Id = p.Id, Name = p.Name, Level = p.Level })
            .ToListAsync();
    }

    public async Task<PositionDto> CreateAsync(CreatePositionDto request)
    {
        var pos = new Position
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Level = request.Level,
            CreatedAt = DateTime.UtcNow
        };

        _context.Positions.Add(pos);
        await _context.SaveChangesAsync();

        return new PositionDto { Id = pos.Id, Name = pos.Name, Level = pos.Level };
    }

    public async Task DeleteAsync(Guid id)
    {
        var pos = await _context.Positions.FindAsync(id);
        if (pos != null)
        {
            _context.Positions.Remove(pos);
            await _context.SaveChangesAsync();
        }
    }
}
