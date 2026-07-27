using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using EMS.Domain.Common.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace EMS.Application.Attendances;

public class ShiftRotationService : IShiftRotationService
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public ShiftRotationService(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<List<ShiftRotationGroupDto>> GetAllGroupsAsync()
    {
        var groups = await _context.ShiftRotationGroups.OrderBy(g => g.Name).ToListAsync();
        return groups.Select(g => new ShiftRotationGroupDto(g.Id, g.Name, g.RotationStartDate.Date)).ToList();
    }

    public async Task<ShiftRotationGroupDto?> GetGroupByIdAsync(Guid id)
    {
        var g = await _context.ShiftRotationGroups.FindAsync(id);
        if (g == null) return null;
        return new ShiftRotationGroupDto(g.Id, g.Name, g.RotationStartDate.Date);
    }

    public async Task<ShiftRotationGroupDto> CreateGroupAsync(CreateShiftRotationGroupRequest request)
    {
        var group = new ShiftRotationGroup
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            RotationStartDate = request.RotationStartDate.Date
        };
        
        _context.ShiftRotationGroups.Add(group);
        await _context.SaveChangesAsync();
        
        return new ShiftRotationGroupDto(group.Id, group.Name, group.RotationStartDate);
    }

    public async Task<ShiftRotationGroupDto> UpdateGroupAsync(Guid id, UpdateShiftRotationGroupRequest request)
    {
        var group = await _context.ShiftRotationGroups.FindAsync(id) 
            ?? throw new NotFoundException("Group not found");
            
        group.Name = request.Name;
        group.RotationStartDate = request.RotationStartDate.Date;
        
        await _context.SaveChangesAsync();
        return new ShiftRotationGroupDto(group.Id, group.Name, group.RotationStartDate);
    }

    public async Task DeleteGroupAsync(Guid id)
    {
        var group = await _context.ShiftRotationGroups.FindAsync(id);
        if (group != null)
        {
            _context.ShiftRotationGroups.Remove(group);
            await _context.SaveChangesAsync();
        }
    }

    public async Task AssignEmployeesToGroupAsync(Guid groupId, List<Guid> employeeIds)
    {
        var group = await _context.ShiftRotationGroups.FindAsync(groupId) 
            ?? throw new NotFoundException("Group not found");
            
        var employees = await _context.Employees.Where(e => employeeIds.Contains(e.Id)).ToListAsync();
        foreach (var emp in employees)
        {
            emp.RotationGroupId = groupId;
        }
        await _context.SaveChangesAsync();
    }

    public async Task<List<ShiftRotationPatternDto>> GetPatternsByGroupIdAsync(Guid groupId)
    {
        var patterns = await _context.ShiftRotationPatterns
            .Include(p => p.WorkShift)
            .Where(p => p.RotationGroupId == groupId)
            .OrderBy(p => p.CycleWeekNumber)
            .ToListAsync();
            
        return patterns.Select(p => new ShiftRotationPatternDto(p.Id, p.RotationGroupId, p.CycleWeekNumber, p.WorkShiftId, p.WorkShift.Name)).ToList();
    }

    public async Task<ShiftRotationPatternDto> CreatePatternAsync(CreateShiftRotationPatternRequest request)
    {
        var exists = await _context.ShiftRotationPatterns.AnyAsync(p => p.RotationGroupId == request.RotationGroupId && p.CycleWeekNumber == request.CycleWeekNumber);
        if (exists) throw new BadRequestException("Pattern for this week number already exists.");
        
        var pattern = new ShiftRotationPattern
        {
            Id = Guid.NewGuid(),
            RotationGroupId = request.RotationGroupId,
            CycleWeekNumber = request.CycleWeekNumber,
            WorkShiftId = request.WorkShiftId
        };
        
        _context.ShiftRotationPatterns.Add(pattern);
        await _context.SaveChangesAsync();
        
        var ws = await _context.WorkShifts.FindAsync(request.WorkShiftId);
        return new ShiftRotationPatternDto(pattern.Id, pattern.RotationGroupId, pattern.CycleWeekNumber, pattern.WorkShiftId, ws?.Name ?? "");
    }

    public async Task<ShiftRotationPatternDto> UpdatePatternAsync(Guid id, UpdateShiftRotationPatternRequest request)
    {
        var pattern = await _context.ShiftRotationPatterns.Include(p => p.WorkShift).FirstOrDefaultAsync(p => p.Id == id) 
            ?? throw new NotFoundException("Pattern not found");
            
        pattern.CycleWeekNumber = request.CycleWeekNumber;
        pattern.WorkShiftId = request.WorkShiftId;
        
        await _context.SaveChangesAsync();
        
        var ws = await _context.WorkShifts.FindAsync(request.WorkShiftId);
        return new ShiftRotationPatternDto(pattern.Id, pattern.RotationGroupId, pattern.CycleWeekNumber, pattern.WorkShiftId, ws?.Name ?? "");
    }

    public async Task DeletePatternAsync(Guid id)
    {
        var pattern = await _context.ShiftRotationPatterns.FindAsync(id);
        if (pattern != null)
        {
            _context.ShiftRotationPatterns.Remove(pattern);
            await _context.SaveChangesAsync();
        }
    }

    public async Task GenerateShiftScheduleAsync(DateTime startDate, DateTime endDate)
    {
        var groups = await _context.ShiftRotationGroups
            .Include(g => g.Patterns)
            .Include(g => g.Employees)
            .ToListAsync();

        var existingSchedules = await _context.ShiftSchedules
            .Where(ss => ss.Date >= startDate.Date && ss.Date <= endDate.Date)
            .ToListAsync();

        var existingSchedulesDict = existingSchedules.ToDictionary(ss => (ss.EmployeeId, ss.Date.Date));

        var currentUserId = _currentUserService.UserId ?? Guid.Empty;

        foreach (var group in groups)
        {
            if (!group.Patterns.Any() || !group.Employees.Any()) continue;

            int cycleLength = group.Patterns.Max(p => p.CycleWeekNumber);
            if (cycleLength <= 0) continue;

            for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
            {
                int diffDays = (int)Math.Floor((date - group.RotationStartDate.Date).TotalDays);
                int weekIndex = (int)Math.Floor((double)diffDays / 7.0);
                
                // Modulo arithmetic for negative weeks (if date is before rotation start date)
                int effectiveWeek = ((weekIndex % cycleLength) + cycleLength) % cycleLength + 1;
                
                var pattern = group.Patterns.FirstOrDefault(p => p.CycleWeekNumber == effectiveWeek);
                if (pattern == null) continue;

                foreach (var emp in group.Employees)
                {
                    var key = (emp.Id, date);
                    existingSchedulesDict.TryGetValue(key, out var existing);
                    
                    if (existing != null)
                    {
                        // Update if not manually overridden and shift is different
                        if (!existing.IsManualOverride && existing.WorkShiftId != pattern.WorkShiftId)
                        {
                            existing.WorkShiftId = pattern.WorkShiftId;
                            // _context.ShiftSchedules.Update(existing); // EF tracks it automatically
                        }
                    }
                    else
                    {
                        // Create new
                        var newSchedule = new ShiftSchedule
                        {
                            Id = Guid.NewGuid(),
                            EmployeeId = emp.Id,
                            Date = date,
                            WorkShiftId = pattern.WorkShiftId,
                            IsManualOverride = false,
                            CreatedBy = currentUserId,
                            CreatedAt = DateTime.UtcNow
                        };
                        _context.ShiftSchedules.Add(newSchedule);
                        existingSchedulesDict[key] = newSchedule; // Add to local dictionary to avoid duplicates in same loop
                    }
                }
            }
        }
        
        await _context.SaveChangesAsync();
    }
}
