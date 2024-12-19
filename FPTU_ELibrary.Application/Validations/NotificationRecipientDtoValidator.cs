using FluentValidation;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Dtos.Notifications;
using FPTU_ELibrary.Application.Extensions;
using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.Application.Validations;

public class NotificationRecipientDtoValidator: AbstractValidator<NotificationRecipientDto>
{
    public NotificationRecipientDtoValidator(string langContext)
    {
        var langEnum =
            (SystemLanguage?) EnumExtensions.GetValueFromDescription<SystemLanguage>(langContext);
        var isEng = langEnum == SystemLanguage.English;
        
        // NotificationId must not be null or empty
        RuleFor(notificationRecipient => notificationRecipient.NotificationId)
            .NotNull()
            .WithMessage(isEng
                ? "NotificationId is required"
                : "Yêu cầu NotificationId")
            .NotEmpty()
            .WithMessage(isEng
                ? "NotificationId cannot be empty"
                : "NotificationId không được phép rỗng");

        // RecipientId must not be null or empty
        RuleFor(notificationRecipient => notificationRecipient.RecipientId)
            .NotNull()
            .WithMessage(isEng
                ? "RecipientId is required"
                : "Yêu cầu RecipientId")
            .NotEmpty()
            .WithMessage(isEng
                ? "RecipientId cannot be empty"
                : "RecipientId không được phép rỗng");;
    }
}