using System;
using System.Collections.Generic;

namespace EMS.Application.Candidates;

public record CandidateDto(
    Guid Id,
    string FullName,
    string Email,
    string? Phone,
    Guid AppliedDepartmentId,
    string AppliedDepartmentName,
    Guid AppliedPositionId,
    string AppliedPositionName,
    string Status,
    string? Notes,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record CreateCandidateRequest(
    string FullName,
    string Email,
    string? Phone,
    Guid AppliedDepartmentId,
    Guid AppliedPositionId,
    string? Notes
);

public record UpdateCandidateRequest(
    string FullName,
    string Email,
    string? Phone,
    Guid AppliedDepartmentId,
    Guid AppliedPositionId,
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
