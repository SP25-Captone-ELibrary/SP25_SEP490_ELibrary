using FluentValidation;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.Application.Validations
{
    public class NotificationDtoValidator : AbstractValidator<NotificationDto>
    {
        public NotificationDtoValidator()
        {
            // Title must not be null or empty
            RuleFor(notification => notification.Title)
                .NotEmpty().WithMessage("Title is required.")
                .NotNull().WithMessage("Title cannot be null.");

            // Message must not be null or empty
            RuleFor(notification => notification.Message)
                .NotEmpty().WithMessage("Message is required.")
                .NotNull().WithMessage("Message cannot be null.");
            // CreatedDate must not be in the future
            RuleFor(notification => notification.CreateDate)
                .Must(date => date <= DateTime.UtcNow)
                .WithMessage("CreateDate cannot be in the future.");

            // CreatedBy must not be null or empty
            RuleFor(notification => notification.CreatedBy)
                .NotEmpty().WithMessage("CreatedBy is required.")
                .NotNull().WithMessage("CreatedBy cannot be null.");

            // NotificationType must not be null or empty and must match a valid enum value
            RuleFor(notification => notification.NotificationType)
                .NotEmpty().WithMessage("NotificationType is required.")
                .NotNull().WithMessage("NotificationType cannot be null.")
                .Must(type => Enum.TryParse(typeof(NotificationType), type, true, out _))
                .WithMessage("NotificationType must be a valid value (e.g., Event, Reminder, Notice).");
        }
    }
}