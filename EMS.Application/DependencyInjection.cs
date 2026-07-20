using Microsoft.Extensions.DependencyInjection;
using FluentValidation;

namespace EMS.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;
        
        services.AddAutoMapper(assembly);
        services.AddValidatorsFromAssembly(assembly);
        
        services.AddScoped<EMS.Application.Auth.IAuthService, EMS.Application.Auth.AuthService>();
        services.AddScoped<EMS.Application.Employees.IEmployeeService, EMS.Application.Employees.EmployeeService>();
        services.AddScoped<EMS.Application.Leaves.ILeaveService, EMS.Application.Leaves.LeaveService>();
        services.AddScoped<EMS.Application.Attendances.IAttendanceService, EMS.Application.Attendances.AttendanceService>();
        services.AddScoped<EMS.Application.DailyReports.IDailyReportService, EMS.Application.DailyReports.DailyReportService>();
        services.AddScoped<EMS.Application.Settings.IDepartmentService, EMS.Application.Settings.DepartmentService>();
        services.AddScoped<EMS.Application.Settings.IPositionService, EMS.Application.Settings.PositionService>();
        services.AddScoped<EMS.Application.Settings.IRoleService, EMS.Application.Settings.RoleService>();
        
        return services;
    }
}
