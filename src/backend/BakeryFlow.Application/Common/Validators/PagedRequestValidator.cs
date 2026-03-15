using BakeryFlow.Application.Common.Models;
using FluentValidation;

namespace BakeryFlow.Application.Common.Validators;

public sealed class PagedRequestValidator : AbstractValidator<PagedRequest>
{
    public PagedRequestValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
