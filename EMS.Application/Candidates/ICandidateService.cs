using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using EMS.Application.Assessments;

namespace EMS.Application.Candidates;

public interface ICandidateService
{
    Task<List<CandidateDto>> GetCandidatesAsync();
    Task<CandidateDto> GetCandidateByIdAsync(Guid id);
    Task<CandidateDto> CreateCandidateAsync(CreateCandidateRequest request);
    Task UpdateCandidateAsync(Guid id, UpdateCandidateRequest request);
    Task UpdateCandidateStatusAsync(Guid id, string status);
    Task<List<AssignedTestLinkDto>> AssignTestAsync(Guid id, AssignTestRequest request);
    Task<List<TestResultDto>> GetCandidateTestResultsAsync(Guid id);
    Task<(Guid EmployeeId, string TempPassword)> ConvertToEmployeeAsync(Guid id, ConvertToEmployeeRequest request);
    
    // Public career portal application
    Task<PublicApplyResultDto> SubmitPublicApplicationAsync(PublicApplyServiceRequest request, string clientIp);
    
    // Documents management
    Task<List<CandidateDocumentDto>> GetCandidateDocumentsAsync(Guid candidateId);
    Task<(Stream FileStream, string ContentType, string FileName)> DownloadCandidateDocumentAsync(Guid candidateId, Guid documentId);
}
