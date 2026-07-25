using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EMS.Application.Attendances;

public class WorkShiftService : IWorkShiftService
{
    private readonly IApplicationDbContext _context;

    public WorkShiftService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<WorkShiftDto>> GetAllWorkShiftsAsync()
    {
        var shifts = await _context.WorkShifts
            .OrderBy(s => s.StartTime)
            .ToListAsync();
            
        return shifts.Select(s => new WorkShiftDto(
            s.Id, s.Name, s.StartTime, s.EndTime, s.IsOvernight, s.ToleranceMinutes)).ToList();
    }

    public async Task<WorkShiftDto?> GetWorkShiftByIdAsync(Guid id)
    {
        var shift = await _context.WorkShifts.FindAsync(id);
        if (shift == null) return null;
        
        return new WorkShiftDto(shift.Id, shift.Name, shift.StartTime, shift.EndTime, shift.IsOvernight, shift.ToleranceMinutes);
    }

    public async Task<WorkShiftDto> CreateWorkShiftAsync(CreateWorkShiftRequest request)
    {
        var shift = new WorkShift
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            IsOvernight = request.IsOvernight,
            ToleranceMinutes = request.ToleranceMinutes,
            CreatedAt = DateTime.UtcNow
        };
        
        _context.WorkShifts.Add(shift);
        await _context.SaveChangesAsync();
        
        return new WorkShiftDto(shift.Id, shift.Name, shift.StartTime, shift.EndTime, shift.IsOvernight, shift.ToleranceMinutes);
    }

    public async Task<WorkShiftDto> UpdateWorkShiftAsync(Guid id, UpdateWorkShiftRequest request)
    {
        var shift = await _context.WorkShifts.FindAsync(id);
        if (shift == null) throw new Exception("WorkShift not found.");
        
        shift.Name = request.Name;
        shift.StartTime = request.StartTime;
        shift.EndTime = request.EndTime;
        shift.IsOvernight = request.IsOvernight;
        shift.ToleranceMinutes = request.ToleranceMinutes;
        shift.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
        
        return new WorkShiftDto(shift.Id, shift.Name, shift.StartTime, shift.EndTime, shift.IsOvernight, shift.ToleranceMinutes);
    }

    public async Task DeleteWorkShiftAsync(Guid id)
    {
        var shift = await _context.WorkShifts.FindAsync(id);
        if (shift == null) throw new Exception("WorkShift not found.");
        
        // Cannot delete if it is being used as default or in schedules
        bool inUse = await _context.Employees.AnyAsync(e => e.DefaultShiftId == id) ||
                     await _context.ShiftSchedules.AnyAsync(ss => ss.WorkShiftId == id);
                     
        if (inUse) throw new Exception("Cannot delete WorkShift because it is currently assigned to one or more employees or schedules.");
        
        _context.WorkShifts.Remove(shift);
        await _context.SaveChangesAsync();
    }

    public async Task<List<ShiftScheduleDto>> GetShiftSchedulesAsync(Guid? employeeId, DateTime startDate, DateTime endDate)
    {
        var query = _context.ShiftSchedules
            .Include(ss => ss.WorkShift)
            .Where(ss => ss.Date >= startDate.Date && ss.Date <= endDate.Date);
            
        if (employeeId.HasValue && employeeId.Value != Guid.Empty)
        {
            query = query.Where(ss => ss.EmployeeId == employeeId.Value);
        }

        var schedules = await query
            .OrderBy(ss => ss.Date)
            .ToListAsync();
            
        return schedules.Select(ss => new ShiftScheduleDto(
            ss.Id,
            ss.EmployeeId,
            ss.Date,
            ss.WorkShiftId,
            new WorkShiftDto(ss.WorkShift.Id, ss.WorkShift.Name, ss.WorkShift.StartTime, ss.WorkShift.EndTime, ss.WorkShift.IsOvernight, ss.WorkShift.ToleranceMinutes),
            ss.IsManualOverride
        )).ToList();
    }

    public async Task AssignShiftScheduleAsync(AssignShiftScheduleRequest request, Guid createdByUserId)
    {
        var targetDate = request.Date.Date;
        
        var existing = await _context.ShiftSchedules
            .FirstOrDefaultAsync(ss => ss.EmployeeId == request.EmployeeId && ss.Date == targetDate);
            
        if (existing != null)
        {
            existing.WorkShiftId = request.WorkShiftId;
            existing.IsManualOverride = true;
        }
        else
        {
            _context.ShiftSchedules.Add(new ShiftSchedule
            {
                Id = Guid.NewGuid(),
                EmployeeId = request.EmployeeId,
                Date = targetDate,
                WorkShiftId = request.WorkShiftId,
                IsManualOverride = true,
                CreatedBy = createdByUserId,
                CreatedAt = DateTime.UtcNow
            });
        }
        
        await _context.SaveChangesAsync();
    }

    public async Task OverrideShiftAsync(Guid id, Guid workShiftId)
    {
        var existing = await _context.ShiftSchedules.FindAsync(id);
        if (existing == null) throw new Exception("Shift Schedule not found.");

        existing.WorkShiftId = workShiftId;
        existing.IsManualOverride = true;
        
        await _context.SaveChangesAsync();
    }
}
