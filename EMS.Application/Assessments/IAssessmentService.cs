using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EMS.Domain.Enums;

namespace EMS.Application.Assessments;

public interface IAssessmentService
{
    Task<List<TestDto>> GetTestsAsync(TestType? type = null);
    Task<List<TestQuestionDto>> GetTestQuestionsAsync(Guid testId);
    Task<TestSessionDto> StartSessionAsync(StartTestSessionRequest request);
    Task<TestSessionDto> GetTestSessionAsync(Guid sessionId);
    Task<VerifyAccessCodeResponse> VerifyAccessCodeAsync(Guid sessionId, string accessCode);
    Task UploadProctoringSnapshotAsync(Guid sessionId, string base64Image);
    Task RecordTabSwitchAsync(Guid sessionId, int count);
    Task SubmitAnswerAsync(Guid sessionId, SubmitAnswerRequest request);
    Task<TestResultDto> SubmitSessionAsync(Guid sessionId);
    Task<TestResultDto> GetTestResultAsync(Guid sessionId);
}
