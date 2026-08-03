using System;

namespace EMS.Application.JobOpenings;

public record JobOpeningDto(
    Guid Id,
    string Title,
    Guid DepartmentId,
    string DepartmentName,
    Guid PositionId,
    string PositionName,
    string Description,
    string Requirements,
    bool IsActive,
    int ApplicantCount,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record PublicJobOpeningDto(
    Guid Id,
    string Title,
    Guid DepartmentId,
    string DepartmentName,
    Guid PositionId,
    string PositionName,
    string Description,
    string Requirements,
    DateTime CreatedAt
);

public record CreateJobOpeningRequest(
    string Title,
    Guid DepartmentId,
    Guid PositionId,
    string Description,
    string Requirements,
    bool IsActive = true
);

public record UpdateJobOpeningRequest(
    string Title,
    Guid DepartmentId,
    Guid PositionId,
    string Description,
    string Requirements,
    bool IsActive
);
