using System;
using EMS.Domain.Enums;

namespace EMS.Domain.Entities;

public class CandidateDocument
{
    public Guid Id { get; set; }
    
    public Guid CandidateId { get; set; }
    public Candidate Candidate { get; set; } = null!;
    
    public CandidateDocumentType DocumentType { get; set; }
    
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public long FileSize { get; set; }
    
    public DateTime UploadedAt { get; set; }
}
