namespace EMS.Domain.Enums;

public enum TestType
{
    Logic = 0,
    Personality = 1
}

public enum TraitCategory
{
    None = 0,
    D = 1,
    I = 2,
    S = 3,
    C = 4
}

public enum TestTakenByType
{
    Candidate = 0,
    Employee = 1
}

public enum TestSessionStatus
{
    InProgress = 0,
    Completed = 1,
    Expired = 2
}

public enum QuestionCategory
{
    None = 0,
    Verbal = 1,
    Numeric = 2,
    Logic = 3
}
