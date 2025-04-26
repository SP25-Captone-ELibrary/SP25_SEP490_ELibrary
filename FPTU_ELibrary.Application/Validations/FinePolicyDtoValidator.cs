using FluentValidation;
using FPTU_ELibrary.Application.Dtos.Fine;
using FPTU_ELibrary.Application.Extensions;
using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.Application.Validations;

public class FinePolicyDtoValidator : AbstractValidator<FinePolicyDto>
{
    public FinePolicyDtoValidator(string langContext)
    {
        // get the language enum from the langContext
        var langEnum =
            (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(langContext);
        // check if the language is English
        var isEng = langEnum == SystemLanguage.English;

        // ConditionType
        RuleFor(finePolicy => finePolicy.ConditionType)
            .NotNull()
            .WithMessage(isEng
                ? "Condition type is required"
                : "Yêu cầu nhập loại điều kiện");
        
        // Title
        RuleFor(x => x.FinePolicyTitle)
            .NotEmpty()
            .MaximumLength(255)
            .WithMessage(isEng
                ? "Title is required and must not exceed 255 characters."
                : "Tiêu đề là bắt buộc và không được vượt quá 255 ký tự.");

        // Description
        RuleFor(x => x.Description)
            .MaximumLength(255)
            .WithMessage(isEng
                ? "Description must not exceed 255 characters."
                : "Mô tả không được vượt quá 255 ký tự.");

        #region ConditionType is Damage
        When(x => x.ConditionType == FinePolicyConditionType.Damage, () =>
        {
            // MinDamagePct
            RuleFor(x => x.MinDamagePct)
                .NotNull()
                .WithMessage(isEng
                    ? "Minimum damage percentage is required"
                    : "Tỷ lệ hư hỏng tối thiểu là bắt buộc")
                .InclusiveBetween(0m, 1m)
                .WithMessage(isEng
                    ? "Minimum damage percentage must be between 0% and 100%"
                    : "Tỷ lệ hư hỏng tối thiểu phải nằm trong khoảng 0% đến 100%");

            // MaxDamagePct
            RuleFor(x => x.MaxDamagePct)
                .NotNull()
                .WithMessage(isEng
                    ? "Maximum damage percentage is required"
                    : "Tỷ lệ hư hỏng tối đa là bắt buộc")
                .InclusiveBetween(0m, 1m)
                .WithMessage(isEng
                    ? "Maximum damage percentage must be between 0% and 100%"
                    : "Tỷ lệ hư hỏng tối đa phải nằm trong khoảng 0% đến 100%");

            // ChargePct
            RuleFor(x => x.ChargePct)
                .NotNull()
                .WithMessage(isEng
                    ? "Charge percentage is required"
                    : "Tỷ lệ tính phí là bắt buộc")
                .InclusiveBetween(0m, 1m)
                .WithMessage(isEng
                    ? "Charge percentage must be between 0% and 100%"
                    : "Tỷ lệ tính phí phải nằm trong khoảng 0% đến 100%");

            // ProcessingFee
            RuleFor(x => x.ProcessingFee)
                .NotNull()
                .WithMessage(isEng
                    ? "Processing fee is required"
                    : "Vui lòng nhập phí xử lý")
                .GreaterThanOrEqualTo(0m)
                .WithMessage(isEng
                    ? "Processing fee must be at least 0"
                    : "Vui lòng nhập phí xử lý");

            // Min ≤ Max
            RuleFor(x => x)
                .Must(x => x.MinDamagePct <= x.MaxDamagePct)
                .When(x => x.MinDamagePct.HasValue && x.MaxDamagePct.HasValue)
                .WithMessage(isEng
                    ? "Minimum damage percentage cannot exceed maximum damage percentage."
                    : "Tỷ lệ hư hỏng tối thiểu không được lớn hơn tỷ lệ hư hỏng tối đa.");
        });
        #endregion

        #region ConditionType is Overdue
        When(x => x.ConditionType == FinePolicyConditionType.OverDue, () =>
        {
            RuleFor(x => x.DailyRate)
                .NotNull()
                .WithMessage(isEng
                    ? "Daily rate is required for overdue policies"
                    : "Mức phạt hàng ngày là bắt buộc cho chính sách quá hạn")
                .GreaterThanOrEqualTo(0m)
                .WithMessage(isEng
                    ? "Daily rate must be at least 0"
                    : "Mức phạt hàng ngày phải lớn hơn hoặc bằng 0");
        });
        #endregion
        
        #region ConditionType is Lost
        When(x => x.ConditionType == FinePolicyConditionType.Lost, () =>
        {
            RuleFor(x => x.ChargePct)
                .NotNull()
                .WithMessage(isEng
                    ? "Replacement fee percentage is required for lost policies"
                    : "Tỷ lệ tính phí là bắt buộc cho chính sách mất")
                .InclusiveBetween(0m, 1m)
                .WithMessage(isEng
                    ? "Replacement fee percentage must be between 0% and 100%"
                    : "Tỷ lệ tính phí phải nằm trong khoảng 0% đến 100%");

            RuleFor(x => x.ProcessingFee)
                .NotNull()
                .WithMessage(isEng
                    ? "Processing fee is required for lost policies"
                    : "Phí xử lý là bắt buộc cho chính sách mất")
                .GreaterThanOrEqualTo(0m)
                .WithMessage(isEng
                    ? "Processing fee must be at least 0"
                    : "Phí xử lý phải lớn hơn hoặc bằng 0");
        });
        #endregion
    }
}