using System;
using System.Collections.Generic;

namespace EMS.Domain.Entities;

public class JobOpening
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    
    public Guid DepartmentId { get; set; }
    public Department Department { get; set; } = null!;
    
    public Guid PositionId { get; set; }
    public Position Position { get; set; } = null!;
    
    public string Description { get; set; } = string.Empty;
    public string Requirements { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    public ICollection<Candidate> Candidates { get; set; } = new List<Candidate>();
}
