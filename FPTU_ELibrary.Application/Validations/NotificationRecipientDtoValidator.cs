using FluentValidation;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Dtos.Notifications;

namespace FPTU_ELibrary.Application.Validations;

public class NotificationRecipientDtoValidator: AbstractValidator<NotificationRecipientDto>
{
    public NotificationRecipientDtoValidator()
    {
        // NotificationId must not be null or empty
        RuleFor(notificationRecipient => notificationRecipient.NotificationId)
            .NotEmpty().WithMessage("NotificationId is required.")
            .NotNull().WithMessage("NotificationId cannot be null.");

        // RecipientId must not be null or empty
        RuleFor(notificationRecipient => notificationRecipient.RecipientId)
            .NotEmpty().WithMessage("RecipientId is required.")
            .NotNull().WithMessage("RecipientId cannot be null.");
    }
}