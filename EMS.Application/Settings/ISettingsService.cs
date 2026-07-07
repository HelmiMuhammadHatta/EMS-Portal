namespace EMS.Application.Settings;

public interface IDepartmentService
{
    Task<List<DepartmentDto>> GetAllAsync();
    Task<DepartmentDto> CreateAsync(CreateDepartmentDto request);
    Task DeleteAsync(Guid id);
}

public interface IPositionService
{
    Task<List<PositionDto>> GetAllAsync();
    Task<PositionDto> CreateAsync(CreatePositionDto request);
    Task DeleteAsync(Guid id);
}
