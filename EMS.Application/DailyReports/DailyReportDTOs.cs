using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using EMS.Domain.Enums;
using EMS.Application.Employees;

namespace EMS.Application.DailyReports;

public class DailyReportDto
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public DateTime ReportDate { get; set; }
    public string TasksCompleted { get; set; } = string.Empty;
    public string? Blockers { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ManagerFeedback { get; set; }
    public Guid? ReviewedBy { get; set; }
    public string? ReviewerName { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateDailyReportDto
{
    [Required]
    public DateTime ReportDate { get; set; }
    [Required]
    public string TasksCompleted { get; set; } = string.Empty;
    public string? Blockers { get; set; }
}

public class UpdateDailyReportDto
{
    [Required]
    public string TasksCompleted { get; set; } = string.Empty;
    public string? Blockers { get; set; }
}

public class ReviewDailyReportDto
{
    [Required]
    public string ManagerFeedback { get; set; } = string.Empty;
}

public class DailyReportListQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public Guid? EmployeeId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Status { get; set; }
}


