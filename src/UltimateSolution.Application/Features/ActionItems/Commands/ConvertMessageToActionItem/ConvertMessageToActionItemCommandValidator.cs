using FluentValidation;

namespace UltimateSolution.Application.Features.ActionItems.Commands.ConvertMessageToActionItem;

public sealed class ConvertMessageToActionItemCommandValidator : AbstractValidator<ConvertMessageToActionItemCommand>
{
    public ConvertMessageToActionItemCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.MessageId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(400);
        RuleFor(x => x.AssigneeUserId).NotEmpty(); // Made mandatory as requested
    }
}
