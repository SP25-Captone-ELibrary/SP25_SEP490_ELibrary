using FluentValidation;
using FPTU_ELibrary.Application.Dtos.Notifications;
using FPTU_ELibrary.Application.Extensions;
using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.Application.Validations
{
    public class NotificationDtoValidator : AbstractValidator<NotificationDto>
    {
        public NotificationDtoValidator(string langContext)
        {
            var langEnum =
                (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(langContext);
            var isEng = langEnum == SystemLanguage.English;

            // Title must not be null or empty
            RuleFor(notification => notification.Title)
                .NotNull()
                .WithMessage(isEng
                    ? "Title is required"
                    : "Yêu cầu nhập tiêu đề")
                .NotEmpty()
                .WithMessage(isEng
                    ? "Title cannot be empty"
                    : "Tiêu đề không được phép rỗng")
                .MaximumLength(150)
                .WithMessage(isEng
                    ? "Notification title must not exceed than 255 characters"
                    : "Tiêu đề thông báo cho phép tối đa 150 ký tự");
            
            // Message must not be null or empty
            RuleFor(notification => notification.Message)
                .NotNull()
                .WithMessage(isEng
                    ? "Message is required"
                    : "Yêu cầu nhập nội dung")
                .NotEmpty()
                .WithMessage(isEng
                    ? "Message cannot be empty"
                    : "Nội dung không được phép rỗng")
                .MaximumLength(4000)
                .WithMessage(isEng
                    ? "Notification body must not exceed than 4000 characters"
                    : "Nội dung thông báo cho phép tối đa 4000 ký tự");

            // CreatedDate must not be in the future
            // RuleFor(notification => notification.CreateDate)
            //     .Must(date => date < DateTime.Now.AddMinutes(5))
            //     .WithMessage(isEng
            //         ? "CreateDate cannot be in the future"
            //         : "Ngày tạo không được nằm trong tương lai");

            // CreatedBy must not be null or empty
            // RuleFor(notification => notification.CreatedBy)
            //     .NotNull()
            //     .WithMessage(isEng
            //         ? "CreatedBy is required"
            //         : "Yêu cầu nhập người tạo")
            //     .NotEmpty()
            //     .WithMessage(isEng
            //         ? "CreatedBy cannot be empty"
            //         : "Người tạo không được phép rỗng");

            // NotificationType must not be null or empty and must match a valid enum value
            // RuleFor(notification => notification.NotificationType)
            //     .NotNull()
            //     .WithMessage(isEng
            //         ? "Notification type is required"
            //         : "Yêu cầu nhập loại thông báo")
            //     .NotEmpty()
            //     .WithMessage(isEng
            //         ? "Notification type cannot be empty"
            //         : "Loại thông báo không được phép rỗng");
        }
    }
}