using System;
using System.Collections.Generic;
using System.IO;

namespace EMS.Application.Candidates;

public record CandidateDocumentDto(
    Guid Id,
    Guid CandidateId,
    string DocumentType,
    string FileName,
    string FilePath,
    long FileSize,
    DateTime UploadedAt
);

public record CandidateDto(
    Guid Id,
    string FullName,
    string Email,
    string? Phone,
    Guid? JobOpeningId,
    string? JobOpeningTitle,
    Guid AppliedDepartmentId,
    string AppliedDepartmentName,
    Guid AppliedPositionId,
    string AppliedPositionName,
    string? Education,
    string? WorkExperience,
    string Source,
    string Status,
    string? Notes,
    List<CandidateDocumentDto> Documents,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record CreateCandidateRequest(
    string FullName,
    string Email,
    string? Phone,
    Guid? JobOpeningId,
    Guid AppliedDepartmentId,
    Guid AppliedPositionId,
    string? Education,
    string? WorkExperience,
    string? Notes
);

public record UpdateCandidateRequest(
    string FullName,
    string Email,
    string? Phone,
    Guid? JobOpeningId,
    Guid AppliedDepartmentId,
    Guid AppliedPositionId,
    string? Education,
    string? WorkExperience,
    string? Notes
);

public record AssignTestRequest(
    List<Guid> TestIds
);

public record ConvertToEmployeeRequest(
    Guid? ManagerId,
    Guid? DefaultShiftId,
    Guid? RotationGroupId
);

public record AssignedTestLinkDto(
    string Link,
    string AccessCode
);

public record UploadCandidateFileItem(
    Stream Stream,
    string FileName,
    long FileSize,
    string DocumentType
);

public record PublicApplyServiceRequest(
    string FullName,
    string Email,
    string? Phone,
    Guid JobOpeningId,
    string? Education,
    string? WorkExperience,
    UploadCandidateFileItem CvFile,
    UploadCandidateFileItem IjazahFile
);

public record PublicApplyResultDto(
    string Message,
    Guid CandidateId
);
