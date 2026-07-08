using EMS.Domain.Enums;
using FluentValidation;

namespace EMS.Application.Employees;

public class CreateEmployeeRequestValidator : AbstractValidator<CreateEmployeeRequest>
{
    public CreateEmployeeRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.DepartmentId).NotEmpty();
        RuleFor(x => x.PositionId).NotEmpty();
        RuleFor(x => x.Gender).NotEmpty().IsEnumName(typeof(Gender), caseSensitive: false);
        RuleFor(x => x.HireDate).NotEmpty();
    }
}

public class UpdateEmployeeRequestValidator : AbstractValidator<UpdateEmployeeRequest>
{
    public UpdateEmployeeRequestValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.DepartmentId).NotEmpty();
        RuleFor(x => x.PositionId).NotEmpty();
        RuleFor(x => x.Gender).NotEmpty().IsEnumName(typeof(Gender), caseSensitive: false);
        RuleFor(x => x.Status).NotEmpty().IsEnumName(typeof(EmployeeStatus), caseSensitive: false);
    }
}
