using System;
using System.Collections.Generic;
using EMS.Domain.Enums;

namespace EMS.Application.Assessments;

public record TestDto(Guid Id, string Name, TestType Type, string Description, int DurationMinutes, bool IsActive);

public record TestQuestionDto(Guid Id, string QuestionText, int QuestionOrder, string? Category, List<TestQuestionOptionDto> Options);

public record TestQuestionOptionDto(Guid Id, string OptionText, int OptionOrder);

public record StartTestSessionRequest(Guid TestId, TestTakenByType TakenByType, Guid TakenById);

public record TestSessionDto(Guid Id, Guid TestId, TestTakenByType TakenByType, Guid TakenById, DateTime StartedAt, DateTime? CompletedAt, TestSessionStatus Status, string? AccessCode, DateTime? LockedUntil = null, int FailedAccessAttempts = 0, int TabSwitchCount = 0);

public record SubmitAnswerRequest(Guid QuestionId, Guid SelectedOptionId);

public record RecordTabSwitchRequest(int TabSwitchCount);

public record TestResultDto(
    Guid Id,
    Guid TestSessionId,
    TestType TestType,
    int TotalQuestions,
    int CorrectAnswers,
    double ScorePercentage,
    double? VerbalScorePercentage,
    double? NumericScorePercentage,
    double? LogicScorePercentage,
    int DurationSeconds,
    int ScoreD,
    int ScoreI,
    int ScoreS,
    int ScoreC,
    string DominantTrait,
    int TabSwitchCount = 0,
    List<string>? ProctoringSnapshots = null
);

public record VerifyAccessCodeRequest(string AccessCode);

public record VerifyAccessCodeResponse(bool Success, string? Status, string? Message);

public class UploadSnapshotRequest 
{ 
    public string Base64Image { get; set; } = string.Empty;
}
