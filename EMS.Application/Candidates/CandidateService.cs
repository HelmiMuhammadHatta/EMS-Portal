using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EMS.Application.Assessments;
using EMS.Application.Employees;
using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using EMS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EMS.Application.Candidates;

public class CandidateService : ICandidateService
{
    private readonly IApplicationDbContext _context;
    private readonly IAssessmentService _assessmentService;
    private readonly IEmployeeService _employeeService;

    public CandidateService(IApplicationDbContext context, IAssessmentService assessmentService, IEmployeeService employeeService)
    {
        _context = context;
        _assessmentService = assessmentService;
        _employeeService = employeeService;
    }

    public async Task<List<CandidateDto>> GetCandidatesAsync()
    {
        var candidates = await _context.Candidates
            .Include(c => c.AppliedDepartment)
            .Include(c => c.AppliedPosition)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        return candidates.Select(MapToDto).ToList();
    }

    public async Task<CandidateDto> GetCandidateByIdAsync(Guid id)
    {
        var candidate = await _context.Candidates
            .Include(c => c.AppliedDepartment)
            .Include(c => c.AppliedPosition)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (candidate == null) throw new Exception("Candidate not found");
        return MapToDto(candidate);
    }

    public async Task<CandidateDto> CreateCandidateAsync(CreateCandidateRequest request)
    {
        var candidate = new Candidate
        {
            Id = Guid.NewGuid(),
            FullName = request.FullName,
            Email = request.Email,
            Phone = request.Phone,
            AppliedDepartmentId = request.AppliedDepartmentId,
            AppliedPositionId = request.AppliedPositionId,
            Status = CandidateStatus.Applied,
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow
        };

        _context.Candidates.Add(candidate);
        await _context.SaveChangesAsync();

        return await GetCandidateByIdAsync(candidate.Id);
    }

    public async Task UpdateCandidateAsync(Guid id, UpdateCandidateRequest request)
    {
        var candidate = await _context.Candidates.FindAsync(id);
        if (candidate == null) throw new Exception("Candidate not found");

        candidate.FullName = request.FullName;
        candidate.Email = request.Email;
        candidate.Phone = request.Phone;
        candidate.AppliedDepartmentId = request.AppliedDepartmentId;
        candidate.AppliedPositionId = request.AppliedPositionId;
        candidate.Notes = request.Notes;
        candidate.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    public async Task UpdateCandidateStatusAsync(Guid id, string status)
    {
        var candidate = await _context.Candidates.FindAsync(id);
        if (candidate == null) throw new Exception("Candidate not found");

        if (!Enum.TryParse<CandidateStatus>(status, true, out var newStatus))
        {
            throw new Exception("Invalid status");
        }

        candidate.Status = newStatus;
        candidate.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    public async Task<List<AssignedTestLinkDto>> AssignTestAsync(Guid id, AssignTestRequest request)
    {
        var candidate = await _context.Candidates.FindAsync(id);
        if (candidate == null) throw new Exception("Candidate not found");

        var sessionLinks = new List<AssignedTestLinkDto>();

        foreach (var testId in request.TestIds)
        {
            var sessionRequest = new StartTestSessionRequest(testId, TestTakenByType.Candidate, candidate.Id);
            var session = await _assessmentService.StartSessionAsync(sessionRequest);
            sessionLinks.Add(new AssignedTestLinkDto($"/take-test/{testId}/{session.Id}", session.AccessCode ?? ""));
        }

        candidate.Status = CandidateStatus.TestAssigned;
        candidate.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return sessionLinks;
    }

    public async Task<List<TestResultDto>> GetCandidateTestResultsAsync(Guid id)
    {
        var candidate = await _context.Candidates.FindAsync(id);
        if (candidate == null) throw new Exception("Candidate not found");

        var sessions = await _context.TestSessions
            .Where(ts => ts.TakenByType == TestTakenByType.Candidate && ts.TakenById == candidate.Id && ts.Status == TestSessionStatus.Completed)
            .Select(ts => ts.Id)
            .ToListAsync();

        var results = new List<TestResultDto>();
        foreach (var sessionId in sessions)
        {
            try
            {
                var result = await _assessmentService.GetTestResultAsync(sessionId);
                results.Add(result);
            }
            catch
            {
                // Result might not be calculated yet if it was just submitted, or error
            }
        }

        return results;
    }

    public async Task<(Guid EmployeeId, string TempPassword)> ConvertToEmployeeAsync(Guid id, ConvertToEmployeeRequest request)
    {
        var candidate = await _context.Candidates.FindAsync(id);
        if (candidate == null) throw new Exception("Candidate not found");
        if (candidate.Status != CandidateStatus.Passed) throw new Exception("Candidate must be in Passed status to convert to Employee");

        var createReq = new CreateEmployeeRequest
        {
            FullName = candidate.FullName,
            Email = candidate.Email,
            Gender = null, // Default
            HireDate = DateTime.UtcNow,
            DepartmentId = candidate.AppliedDepartmentId,
            PositionId = candidate.AppliedPositionId,
            ManagerId = request.ManagerId,
            DefaultShiftId = request.DefaultShiftId,
            RotationGroupId = request.RotationGroupId,
            CandidateId = candidate.Id
        };

        var (employeeId, tempPassword) = await _employeeService.CreateEmployeeAsync(createReq);

        candidate.Status = CandidateStatus.Hired;
        candidate.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return (employeeId, tempPassword);
    }

    private CandidateDto MapToDto(Candidate c)
    {
        return new CandidateDto(
            c.Id,
            c.FullName,
            c.Email,
            c.Phone,
            c.AppliedDepartmentId,
            c.AppliedDepartment?.Name ?? "",
            c.AppliedPositionId,
            c.AppliedPosition?.Name ?? "",
            c.Status.ToString(),
            c.Notes,
            c.CreatedAt,
            c.UpdatedAt
        );
    }
}
