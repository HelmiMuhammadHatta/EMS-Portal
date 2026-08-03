using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EMS.Application.JobOpenings;

public class JobOpeningService : IJobOpeningService
{
    private readonly IApplicationDbContext _context;

    public JobOpeningService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<JobOpeningDto>> GetAllJobOpeningsAsync()
    {
        var list = await _context.JobOpenings
            .Include(j => j.Department)
            .Include(j => j.Position)
            .Include(j => j.Candidates)
            .OrderByDescending(j => j.CreatedAt)
            .ToListAsync();

        return list.Select(MapToDto).ToList();
    }

    public async Task<JobOpeningDto> GetJobOpeningByIdAsync(Guid id)
    {
        var job = await _context.JobOpenings
            .Include(j => j.Department)
            .Include(j => j.Position)
            .Include(j => j.Candidates)
            .FirstOrDefaultAsync(j => j.Id == id);

        if (job == null) throw new Exception("Job Opening not found");
        return MapToDto(job);
    }

    public async Task<JobOpeningDto> CreateJobOpeningAsync(CreateJobOpeningRequest request)
    {
        var job = new JobOpening
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            DepartmentId = request.DepartmentId,
            PositionId = request.PositionId,
            Description = request.Description,
            Requirements = request.Requirements,
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _context.JobOpenings.Add(job);
        await _context.SaveChangesAsync();

        return await GetJobOpeningByIdAsync(job.Id);
    }

    public async Task UpdateJobOpeningAsync(Guid id, UpdateJobOpeningRequest request)
    {
        var job = await _context.JobOpenings.FindAsync(id);
        if (job == null) throw new Exception("Job Opening not found");

        job.Title = request.Title;
        job.DepartmentId = request.DepartmentId;
        job.PositionId = request.PositionId;
        job.Description = request.Description;
        job.Requirements = request.Requirements;
        job.IsActive = request.IsActive;
        job.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    public async Task DeleteJobOpeningAsync(Guid id)
    {
        var job = await _context.JobOpenings.FindAsync(id);
        if (job == null) throw new Exception("Job Opening not found");

        _context.JobOpenings.Remove(job);
        await _context.SaveChangesAsync();
    }

    public async Task ToggleJobOpeningStatusAsync(Guid id)
    {
        var job = await _context.JobOpenings.FindAsync(id);
        if (job == null) throw new Exception("Job Opening not found");

        job.IsActive = !job.IsActive;
        job.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task<List<PublicJobOpeningDto>> GetPublicJobOpeningsAsync()
    {
        var list = await _context.JobOpenings
            .Include(j => j.Department)
            .Include(j => j.Position)
            .Where(j => j.IsActive)
            .OrderByDescending(j => j.CreatedAt)
            .ToListAsync();

        return list.Select(j => new PublicJobOpeningDto(
            j.Id,
            j.Title,
            j.DepartmentId,
            j.Department?.Name ?? "",
            j.PositionId,
            j.Position?.Name ?? "",
            j.Description,
            j.Requirements,
            j.CreatedAt
        )).ToList();
    }

    public async Task<PublicJobOpeningDto> GetPublicJobOpeningByIdAsync(Guid id)
    {
        var job = await _context.JobOpenings
            .Include(j => j.Department)
            .Include(j => j.Position)
            .FirstOrDefaultAsync(j => j.Id == id && j.IsActive);

        if (job == null) throw new Exception("Lowongan kerja tidak ditemukan atau sudah ditutup.");

        return new PublicJobOpeningDto(
            job.Id,
            job.Title,
            job.DepartmentId,
            job.Department?.Name ?? "",
            job.PositionId,
            job.Position?.Name ?? "",
            job.Description,
            job.Requirements,
            job.CreatedAt
        );
    }

    private static JobOpeningDto MapToDto(JobOpening j) => new(
        j.Id,
        j.Title,
        j.DepartmentId,
        j.Department?.Name ?? "",
        j.PositionId,
        j.Position?.Name ?? "",
        j.Description,
        j.Requirements,
        j.IsActive,
        j.Candidates?.Count ?? 0,
        j.CreatedAt,
        j.UpdatedAt
    );
}
