using System;
using System.Collections.Generic;

namespace EMS.Domain.Entities;

public class ShiftRotationGroup
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    
    // We use DateTime but treat it as a Date
    public DateTime RotationStartDate { get; set; }

    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
    public ICollection<ShiftRotationPattern> Patterns { get; set; } = new List<ShiftRotationPattern>();
}
