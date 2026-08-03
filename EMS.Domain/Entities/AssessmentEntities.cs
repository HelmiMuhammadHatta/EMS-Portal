using System;
using System.Collections.Generic;
using EMS.Domain.Enums;

namespace EMS.Domain.Entities;

public class Test
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public TestType Type { get; set; }
    public string Description { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public bool IsActive { get; set; }
    
    public ICollection<TestQuestion> Questions { get; set; } = new List<TestQuestion>();
}

public class TestQuestion
{
    public Guid Id { get; set; }
    public Guid TestId { get; set; }
    public Test Test { get; set; } = null!;
    
    public string QuestionText { get; set; } = string.Empty;
    public int QuestionOrder { get; set; }
    public QuestionCategory? Category { get; set; }
    
    public ICollection<TestQuestionOption> Options { get; set; } = new List<TestQuestionOption>();
}

public class TestQuestionOption
{
    public Guid Id { get; set; }
    public Guid TestQuestionId { get; set; }
    public TestQuestion TestQuestion { get; set; } = null!;
    
    public string OptionText { get; set; } = string.Empty;
    public int OptionOrder { get; set; }
    
    // For Logic tests
    public bool IsCorrect { get; set; }
    
    // For Personality tests
    public TraitCategory TraitCategory { get; set; }
}

public class TestSession
{
    public Guid Id { get; set; }
    public Guid TestId { get; set; }
    public Test Test { get; set; } = null!;
    
    public TestTakenByType TakenByType { get; set; }
    public Guid TakenById { get; set; } // CandidateId or EmployeeId
    
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    
    public TestSessionStatus Status { get; set; }
    
    public string? AccessCode { get; set; }
    public int FailedAccessAttempts { get; set; }
    public DateTime? LockedUntil { get; set; }
    public int TabSwitchCount { get; set; }
    
    public ICollection<TestAnswer> Answers { get; set; } = new List<TestAnswer>();
    public TestResult? Result { get; set; }
    public ICollection<ProctoringSnapshot> ProctoringSnapshots { get; set; } = new List<ProctoringSnapshot>();
}

public class TestAnswer
{
    public Guid Id { get; set; }
    public Guid TestSessionId { get; set; }
    public TestSession TestSession { get; set; } = null!;
    
    public Guid TestQuestionId { get; set; }
    public TestQuestion TestQuestion { get; set; } = null!;
    
    public Guid SelectedOptionId { get; set; }
    public TestQuestionOption SelectedOption { get; set; } = null!;
}

public class TestResult
{
    public Guid Id { get; set; }
    public Guid TestSessionId { get; set; }
    public TestSession TestSession { get; set; } = null!;
    
    public TestType TestType { get; set; }
    
    // For Logic Test
    public int TotalQuestions { get; set; }
    public int CorrectAnswers { get; set; }
    public double ScorePercentage { get; set; }
    public double? VerbalScorePercentage { get; set; }
    public double? NumericScorePercentage { get; set; }
    public double? LogicScorePercentage { get; set; }
    public int DurationSeconds { get; set; }
    
    // For Personality Test (DISC)
    public int ScoreD { get; set; }
    public int ScoreI { get; set; }
    public int ScoreS { get; set; }
    public int ScoreC { get; set; }
    public TraitCategory DominantTrait { get; set; }

    // Proctoring Metrics
    public int TabSwitchCount { get; set; }
}

public class ProctoringSnapshot
{
    public Guid Id { get; set; }
    public Guid TestSessionId { get; set; }
    public TestSession TestSession { get; set; } = null!;
    
    public string FilePath { get; set; } = string.Empty;
    public DateTime CapturedAt { get; set; }
}
