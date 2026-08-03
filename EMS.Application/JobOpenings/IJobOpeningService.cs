using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EMS.Application.JobOpenings;

public interface IJobOpeningService
{
    Task<List<JobOpeningDto>> GetAllJobOpeningsAsync();
    Task<JobOpeningDto> GetJobOpeningByIdAsync(Guid id);
    Task<JobOpeningDto> CreateJobOpeningAsync(CreateJobOpeningRequest request);
    Task UpdateJobOpeningAsync(Guid id, UpdateJobOpeningRequest request);
    Task DeleteJobOpeningAsync(Guid id);
    Task ToggleJobOpeningStatusAsync(Guid id);
    
    // Public queries
    Task<List<PublicJobOpeningDto>> GetPublicJobOpeningsAsync();
    Task<PublicJobOpeningDto> GetPublicJobOpeningByIdAsync(Guid id);
}
