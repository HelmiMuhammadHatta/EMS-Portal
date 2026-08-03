using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using EMS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EMS.Application.Assessments;

public class AssessmentService : IAssessmentService
{
    private readonly IApplicationDbContext _context;

    public AssessmentService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<TestDto>> GetTestsAsync(TestType? type = null)
    {
        var query = _context.Tests.Where(t => t.IsActive);
        if (type.HasValue)
        {
            query = query.Where(t => t.Type == type.Value);
        }

        var tests = await query.ToListAsync();
        return tests.Select(t => new TestDto(t.Id, t.Name, t.Type, t.Description, t.DurationMinutes, t.IsActive)).ToList();
    }

    public async Task<List<TestQuestionDto>> GetTestQuestionsAsync(Guid testId)
    {
        var test = await _context.Tests
            .Include(t => t.Questions)
                .ThenInclude(q => q.Options)
            .FirstOrDefaultAsync(t => t.Id == testId);

        if (test == null) throw new Exception("Test not found");

        return test.Questions
            .OrderBy(q => q.QuestionOrder)
            .Select(q => new TestQuestionDto(
                q.Id, 
                q.QuestionText, 
                q.QuestionOrder, 
                q.Category?.ToString(),
                q.Options.OrderBy(o => o.OptionOrder).Select(o => new TestQuestionOptionDto(o.Id, o.OptionText, o.OptionOrder)).ToList()
            )).ToList();
    }

    public async Task<TestSessionDto> StartSessionAsync(StartTestSessionRequest request)
    {
        var test = await _context.Tests.FindAsync(request.TestId);
        if (test == null) throw new Exception("Test not found");

        var session = new TestSession
        {
            Id = Guid.NewGuid(),
            TestId = request.TestId,
            TakenByType = request.TakenByType,
            TakenById = request.TakenById,
            StartedAt = DateTime.UtcNow,
            Status = TestSessionStatus.InProgress,
            AccessCode = Random.Shared.Next(100000, 999999).ToString()
        };

        _context.TestSessions.Add(session);
        await _context.SaveChangesAsync();

        return new TestSessionDto(session.Id, session.TestId, session.TakenByType, session.TakenById, session.StartedAt, session.CompletedAt, session.Status, session.AccessCode, session.LockedUntil, session.FailedAccessAttempts, session.TabSwitchCount);
    }

    public async Task<TestSessionDto> GetTestSessionAsync(Guid sessionId)
    {
        var session = await _context.TestSessions.FindAsync(sessionId);
        if (session == null) throw new Exception("Session not found");
        return new TestSessionDto(session.Id, session.TestId, session.TakenByType, session.TakenById, session.StartedAt, session.CompletedAt, session.Status, session.AccessCode, session.LockedUntil, session.FailedAccessAttempts, session.TabSwitchCount);
    }

    public async Task<VerifyAccessCodeResponse> VerifyAccessCodeAsync(Guid sessionId, string accessCode)
    {
        var session = await _context.TestSessions.FindAsync(sessionId);
        if (session == null) return new VerifyAccessCodeResponse(false, null, "Sesi test tidak ditemukan");

        if (session.Status == TestSessionStatus.Completed) 
            return new VerifyAccessCodeResponse(false, "Completed", $"Test sudah pernah diselesaikan pada {session.CompletedAt:dd/MM/yyyy HH:mm} UTC. Terima kasih atas partisipasi Anda.");

        if (session.LockedUntil.HasValue && session.LockedUntil.Value > DateTime.UtcNow)
        {
            var remaining = (int)Math.Ceiling((session.LockedUntil.Value - DateTime.UtcNow).TotalMinutes);
            return new VerifyAccessCodeResponse(false, "Locked", $"Sesi terkunci karena terlalu banyak percobaan salah. Silakan coba lagi dalam {remaining} menit.");
        }

        if (session.AccessCode != accessCode?.Trim())
        {
            session.FailedAccessAttempts++;
            if (session.FailedAccessAttempts >= 5)
            {
                session.LockedUntil = DateTime.UtcNow.AddMinutes(15);
                await _context.SaveChangesAsync();
                return new VerifyAccessCodeResponse(false, "Locked", "Kode akses salah 5 kali. Sesi dikunci sementara selama 15 menit.");
            }
            await _context.SaveChangesAsync();
            return new VerifyAccessCodeResponse(false, "InvalidCode", $"Kode akses salah. Sisa percobaan: {5 - session.FailedAccessAttempts} kali.");
        }

        // Success, reset attempts
        session.FailedAccessAttempts = 0;
        session.LockedUntil = null;
        await _context.SaveChangesAsync();

        return new VerifyAccessCodeResponse(true, "Granted", "Akses diberikan.");
    }

    public async Task UploadProctoringSnapshotAsync(Guid sessionId, string base64Image)
    {
        var session = await _context.TestSessions.FindAsync(sessionId);
        if (session == null) throw new Exception("Session not found");

        if (string.IsNullOrEmpty(base64Image)) return;

        try
        {
            // Extract base64 data (remove data:image/jpeg;base64, if present)
            var base64Data = base64Image;
            if (base64Image.Contains(","))
            {
                base64Data = base64Image.Split(',')[1];
            }

            byte[] imageBytes = Convert.FromBase64String(base64Data);

            // Ensure directory exists
            var directoryPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "proctoring", sessionId.ToString());
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            var fileName = $"snapshot_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid().ToString().Substring(0, 6)}.jpg";
            var filePath = Path.Combine(directoryPath, fileName);

            await File.WriteAllBytesAsync(filePath, imageBytes);

            var snapshot = new ProctoringSnapshot
            {
                Id = Guid.NewGuid(),
                TestSessionId = sessionId,
                FilePath = $"/uploads/proctoring/{sessionId}/{fileName}",
                CapturedAt = DateTime.UtcNow
            };

            _context.ProctoringSnapshots.Add(snapshot);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error uploading proctoring snapshot: {ex.Message}");
        }
    }

    public async Task SubmitAnswerAsync(Guid sessionId, SubmitAnswerRequest request)
    {
        var session = await _context.TestSessions
            .Include(ts => ts.Test)
            .FirstOrDefaultAsync(ts => ts.Id == sessionId);

        if (session == null) throw new Exception("Session not found");
        if (session.Status != TestSessionStatus.InProgress) throw new Exception("Session is not in progress");

        if (DateTime.UtcNow > session.StartedAt.AddMinutes(session.Test.DurationMinutes).AddMinutes(1))
        {
            session.Status = TestSessionStatus.Expired;
            await _context.SaveChangesAsync();
            throw new Exception("Test session has expired");
        }

        var answer = await _context.TestAnswers.FirstOrDefaultAsync(ta => ta.TestSessionId == sessionId && ta.TestQuestionId == request.QuestionId);
        if (answer == null)
        {
            answer = new TestAnswer
            {
                Id = Guid.NewGuid(),
                TestSessionId = sessionId,
                TestQuestionId = request.QuestionId,
                SelectedOptionId = request.SelectedOptionId
            };
            _context.TestAnswers.Add(answer);
        }
        else
        {
            answer.SelectedOptionId = request.SelectedOptionId;
        }

        await _context.SaveChangesAsync();
    }

    public async Task RecordTabSwitchAsync(Guid sessionId, int count)
    {
        var session = await _context.TestSessions.FindAsync(sessionId);
        if (session != null && session.Status != TestSessionStatus.Completed)
        {
            session.TabSwitchCount = Math.Max(session.TabSwitchCount, count);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<TestResultDto> SubmitSessionAsync(Guid sessionId)
    {
        var session = await _context.TestSessions
            .Include(ts => ts.Test)
            .Include(ts => ts.Answers)
            .FirstOrDefaultAsync(ts => ts.Id == sessionId);

        if (session == null) throw new Exception("Session not found");
        if (session.Status == TestSessionStatus.Completed) throw new Exception("Test sudah pernah diselesaikan dan tidak dapat dikerjakan ulang.");

        // Check time limit with a slight grace period (e.g. 2 minutes).
        if (DateTime.UtcNow > session.StartedAt.AddMinutes(session.Test.DurationMinutes).AddMinutes(2))
        {
            session.Status = TestSessionStatus.Expired;
            await _context.SaveChangesAsync();
            throw new Exception("Waktu pengerjaan test telah habis.");
        }

        session.CompletedAt = DateTime.UtcNow;
        session.Status = TestSessionStatus.Completed;

        // Calculate Result
        var result = await CalculateTestResultInternalAsync(session);
        _context.TestResults.Add(result);
        
        await _context.SaveChangesAsync();

        return MapToDto(result);
    }

    private async Task<TestResult> CalculateTestResultInternalAsync(TestSession session)
    {
        var allQuestions = await _context.TestQuestions.Where(q => q.TestId == session.TestId).ToListAsync();
        var totalQuestions = allQuestions.Count;
        var answersWithDetails = await _context.TestAnswers
            .Include(a => a.SelectedOption)
            .Include(a => a.TestQuestion)
            .Where(a => a.TestSessionId == session.Id)
            .ToListAsync();

        var durationSeconds = (int)((session.CompletedAt ?? DateTime.UtcNow) - session.StartedAt).TotalSeconds;

        var result = new TestResult
        {
            Id = Guid.NewGuid(),
            TestSessionId = session.Id,
            TestType = session.Test.Type,
            TotalQuestions = totalQuestions,
            DurationSeconds = durationSeconds,
            TabSwitchCount = session.TabSwitchCount
        };

        if (session.Test.Type == TestType.Logic)
        {
            int correctCount = answersWithDetails.Count(a => a.SelectedOption != null && a.SelectedOption.IsCorrect);
            result.CorrectAnswers = correctCount;
            result.ScorePercentage = totalQuestions > 0 ? (double)correctCount / totalQuestions * 100 : 0;
            
            var verbalQuestions = allQuestions.Where(q => q.Category == QuestionCategory.Verbal).ToList();
            if (verbalQuestions.Any())
            {
                var verbalIds = verbalQuestions.Select(q => q.Id).ToHashSet();
                int correctVerbal = answersWithDetails.Count(a => verbalIds.Contains(a.TestQuestionId) && a.SelectedOption != null && a.SelectedOption.IsCorrect);
                result.VerbalScorePercentage = (double)correctVerbal / verbalQuestions.Count * 100;
            }
            else result.VerbalScorePercentage = 0;
            
            var numericQuestions = allQuestions.Where(q => q.Category == QuestionCategory.Numeric).ToList();
            if (numericQuestions.Any())
            {
                var numericIds = numericQuestions.Select(q => q.Id).ToHashSet();
                int correctNumeric = answersWithDetails.Count(a => numericIds.Contains(a.TestQuestionId) && a.SelectedOption != null && a.SelectedOption.IsCorrect);
                result.NumericScorePercentage = (double)correctNumeric / numericQuestions.Count * 100;
            }
            else result.NumericScorePercentage = 0;
            
            var logicQuestions = allQuestions.Where(q => q.Category == QuestionCategory.Logic).ToList();
            if (logicQuestions.Any())
            {
                var logicIds = logicQuestions.Select(q => q.Id).ToHashSet();
                int correctLogic = answersWithDetails.Count(a => logicIds.Contains(a.TestQuestionId) && a.SelectedOption != null && a.SelectedOption.IsCorrect);
                result.LogicScorePercentage = (double)correctLogic / logicQuestions.Count * 100;
            }
            else result.LogicScorePercentage = 0;
        }
        else if (session.Test.Type == TestType.Personality)
        {
            result.ScoreD = answersWithDetails.Count(a => a.SelectedOption != null && a.SelectedOption.TraitCategory == TraitCategory.D);
            result.ScoreI = answersWithDetails.Count(a => a.SelectedOption != null && a.SelectedOption.TraitCategory == TraitCategory.I);
            result.ScoreS = answersWithDetails.Count(a => a.SelectedOption != null && a.SelectedOption.TraitCategory == TraitCategory.S);
            result.ScoreC = answersWithDetails.Count(a => a.SelectedOption != null && a.SelectedOption.TraitCategory == TraitCategory.C);

            // Find max score
            int maxScore = Math.Max(result.ScoreD, Math.Max(result.ScoreI, Math.Max(result.ScoreS, result.ScoreC)));
            if (maxScore == result.ScoreD) result.DominantTrait = TraitCategory.D;
            else if (maxScore == result.ScoreI) result.DominantTrait = TraitCategory.I;
            else if (maxScore == result.ScoreS) result.DominantTrait = TraitCategory.S;
            else result.DominantTrait = TraitCategory.C;
        }

        return result;
    }

    public async Task<TestResultDto> GetTestResultAsync(Guid sessionId)
    {
        var result = await _context.TestResults
            .Include(r => r.TestSession)
            .ThenInclude(ts => ts.ProctoringSnapshots)
            .FirstOrDefaultAsync(r => r.TestSessionId == sessionId);
        if (result == null) throw new Exception("Result not found");

        return MapToDto(result);
    }

    private TestResultDto MapToDto(TestResult result)
    {
        return new TestResultDto(
            result.Id,
            result.TestSessionId,
            result.TestType,
            result.TotalQuestions,
            result.CorrectAnswers,
            result.ScorePercentage,
            result.VerbalScorePercentage,
            result.NumericScorePercentage,
            result.LogicScorePercentage,
            result.DurationSeconds,
            result.ScoreD,
            result.ScoreI,
            result.ScoreS,
            result.ScoreC,
            result.DominantTrait.ToString(),
            result.TabSwitchCount,
            result.TestSession?.ProctoringSnapshots?.OrderBy(p => p.CapturedAt).Select(p => p.FilePath).ToList()
        );
    }
}
