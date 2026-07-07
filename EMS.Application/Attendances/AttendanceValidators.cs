using FluentValidation;

namespace EMS.Application.Attendances;

public class ClockInRequestValidator : AbstractValidator<ClockInRequest>
{
    public ClockInRequestValidator()
    {
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180);
    }
}

public class ClockOutRequestValidator : AbstractValidator<ClockOutRequest>
{
    public ClockOutRequestValidator()
    {
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180);
    }
}
