using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EMS.Application.Assessments;
using EMS.Application.Common;
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
    private readonly IPublicApplyRateLimiter _rateLimiter;

    public CandidateService(
        IApplicationDbContext context, 
        IAssessmentService assessmentService, 
        IEmployeeService employeeService,
        IPublicApplyRateLimiter rateLimiter)
    {
        _context = context;
        _assessmentService = assessmentService;
        _employeeService = employeeService;
        _rateLimiter = rateLimiter;
    }

    public async Task<List<CandidateDto>> GetCandidatesAsync()
    {
        var candidates = await _context.Candidates
            .Include(c => c.AppliedDepartment)
            .Include(c => c.AppliedPosition)
            .Include(c => c.JobOpening)
            .Include(c => c.Documents)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        return candidates.Select(MapToDto).ToList();
    }

    public async Task<CandidateDto> GetCandidateByIdAsync(Guid id)
    {
        var candidate = await _context.Candidates
            .Include(c => c.AppliedDepartment)
            .Include(c => c.AppliedPosition)
            .Include(c => c.JobOpening)
            .Include(c => c.Documents)
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
            JobOpeningId = request.JobOpeningId,
            AppliedDepartmentId = request.AppliedDepartmentId,
            AppliedPositionId = request.AppliedPositionId,
            Education = request.Education,
            WorkExperience = request.WorkExperience,
            Source = CandidateSource.ManualHR,
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
        candidate.JobOpeningId = request.JobOpeningId;
        candidate.AppliedDepartmentId = request.AppliedDepartmentId;
        candidate.AppliedPositionId = request.AppliedPositionId;
        candidate.Education = request.Education;
        candidate.WorkExperience = request.WorkExperience;
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

    public async Task<PublicApplyResultDto> SubmitPublicApplicationAsync(PublicApplyServiceRequest request, string clientIp)
    {
        // 1. Rate Limiting Check
        if (!_rateLimiter.AllowSubmission(clientIp))
        {
            throw new Exception("Batas pengiriman lamaran tercapai (maksimal 3 kali per jam). Silakan coba lagi nanti.");
        }

        // 2. Validate Job Opening
        var job = await _context.JobOpenings.FirstOrDefaultAsync(j => j.Id == request.JobOpeningId && j.IsActive);
        if (job == null)
        {
            throw new Exception("Lowongan pekerjaan tidak ditemukan atau sudah ditutup.");
        }

        // 3. Validate duplicate application
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var isDuplicate = await _context.Candidates.AnyAsync(c => 
            c.Email.ToLower() == normalizedEmail && c.JobOpeningId == request.JobOpeningId);
        
        if (isDuplicate)
        {
            throw new Exception("Anda sudah pernah melamar untuk posisi ini sebelumnya.");
        }

        // 4. Validate Files
        ValidateUploadedFile(request.CvFile, "CV");
        ValidateUploadedFile(request.IjazahFile, "Ijazah");

        // 5. Create Candidate
        var candidateId = Guid.NewGuid();
        var candidate = new Candidate
        {
            Id = candidateId,
            FullName = request.FullName.Trim(),
            Email = normalizedEmail,
            Phone = request.Phone?.Trim(),
            JobOpeningId = job.Id,
            AppliedDepartmentId = job.DepartmentId,
            AppliedPositionId = job.PositionId,
            Education = request.Education?.Trim(),
            WorkExperience = request.WorkExperience?.Trim(),
            Source = CandidateSource.PublicForm,
            Status = CandidateStatus.Applied,
            CreatedAt = DateTime.UtcNow
        };

        _context.Candidates.Add(candidate);

        // 6. Save Files & Document Entities
        var cvDoc = await SaveCandidateDocumentAsync(candidateId, request.CvFile, CandidateDocumentType.CV);
        var ijazahDoc = await SaveCandidateDocumentAsync(candidateId, request.IjazahFile, CandidateDocumentType.Ijazah);

        _context.CandidateDocuments.Add(cvDoc);
        _context.CandidateDocuments.Add(ijazahDoc);

        await _context.SaveChangesAsync();

        return new PublicApplyResultDto(
            "Lamaran Anda telah kami terima, tim HR akan menghubungi Anda jika lolos seleksi awal",
            candidateId
        );
    }

    public async Task<List<CandidateDocumentDto>> GetCandidateDocumentsAsync(Guid candidateId)
    {
        var docs = await _context.CandidateDocuments
            .Where(d => d.CandidateId == candidateId)
            .OrderBy(d => d.UploadedAt)
            .ToListAsync();

        return docs.Select(d => new CandidateDocumentDto(
            d.Id,
            d.CandidateId,
            d.DocumentType.ToString(),
            d.FileName,
            d.FilePath,
            d.FileSize,
            d.UploadedAt
        )).ToList();
    }

    public async Task<(Stream FileStream, string ContentType, string FileName)> DownloadCandidateDocumentAsync(Guid candidateId, Guid documentId)
    {
        var doc = await _context.CandidateDocuments
            .FirstOrDefaultAsync(d => d.Id == documentId && d.CandidateId == candidateId);

        if (doc == null) throw new Exception("Dokumen tidak ditemukan.");

        var webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var physicalPath = Path.Combine(webRoot, doc.FilePath.TrimStart('/'));

        if (!File.Exists(physicalPath))
        {
            throw new Exception("File fisik dokumen tidak ditemukan di server.");
        }

        var ext = Path.GetExtension(physicalPath).ToLowerInvariant();
        var contentType = ext switch
        {
            ".pdf" => "application/pdf",
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            _ => "application/octet-stream"
        };

        var stream = new FileStream(physicalPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return (stream, contentType, doc.FileName);
    }

    private static void ValidateUploadedFile(UploadCandidateFileItem file, string fileLabel)
    {
        if (file == null || file.Stream == null || file.FileSize <= 0)
        {
            throw new Exception($"File {fileLabel} wajib diunggah.");
        }

        const long maxBytes = 5 * 1024 * 1024; // 5 MB
        if (file.FileSize > maxBytes)
        {
            throw new Exception($"Ukuran file {fileLabel} melebihi batas maksimal 5 MB.");
        }

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
        if (!allowedExtensions.Contains(ext))
        {
            throw new Exception($"Format file {fileLabel} tidak didukung. Format yang diizinkan: PDF, JPG, JPEG, PNG.");
        }
    }

    private static async Task<CandidateDocument> SaveCandidateDocumentAsync(Guid candidateId, UploadCandidateFileItem file, CandidateDocumentType docType)
    {
        var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "candidates", candidateId.ToString());
        if (!Directory.Exists(uploadDir))
        {
            Directory.CreateDirectory(uploadDir);
        }

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var uniqueFileName = $"{docType}_{Guid.NewGuid()}{ext}";
        var fullPath = Path.Combine(uploadDir, uniqueFileName);

        using (var destStream = new FileStream(fullPath, FileMode.Create))
        {
            file.Stream.Position = 0;
            await file.Stream.CopyToAsync(destStream);
        }

        return new CandidateDocument
        {
            Id = Guid.NewGuid(),
            CandidateId = candidateId,
            DocumentType = docType,
            FileName = file.FileName,
            FilePath = $"/uploads/candidates/{candidateId}/{uniqueFileName}",
            FileSize = file.FileSize,
            UploadedAt = DateTime.UtcNow
        };
    }

    private static CandidateDto MapToDto(Candidate c)
    {
        return new CandidateDto(
            c.Id,
            c.FullName,
            c.Email,
            c.Phone,
            c.JobOpeningId,
            c.JobOpening?.Title,
            c.AppliedDepartmentId,
            c.AppliedDepartment?.Name ?? "",
            c.AppliedPositionId,
            c.AppliedPosition?.Name ?? "",
            c.Education,
            c.WorkExperience,
            c.Source.ToString(),
            c.Status.ToString(),
            c.Notes,
            c.Documents?.Select(d => new CandidateDocumentDto(
                d.Id,
                d.CandidateId,
                d.DocumentType.ToString(),
                d.FileName,
                d.FilePath,
                d.FileSize,
                d.UploadedAt
            )).ToList() ?? new List<CandidateDocumentDto>(),
            c.CreatedAt,
            c.UpdatedAt
        );
    }
}
