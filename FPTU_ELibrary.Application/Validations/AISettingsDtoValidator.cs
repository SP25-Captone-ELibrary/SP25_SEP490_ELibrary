using FluentValidation;
using FPTU_ELibrary.Application.Dtos.AdminConfiguration;
using FPTU_ELibrary.Application.Extensions;
using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.Application.Validations;

public class AISettingsDtoValidator : AbstractValidator<AISettingsDto>
{
    public AISettingsDtoValidator(string langContext)
    {
        var langEnum =
            (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(langContext);
        var isEng = langEnum == SystemLanguage.English;
        RuleFor(x => x.AuthorNamePercentage)
            .InclusiveBetween(0, 1).WithMessage(isEng
                ? "AuthorNamePercentage must be between 0 and 1."
                : "Tỉ lệ tên tác giả phải trong khoảng từ 0 đến 1");

        RuleFor(x => x.TitlePercentage)
            .InclusiveBetween(0, 1).WithMessage(isEng
                ? "TitlePercentage must be between 0 and 1."
                : "Tỉ lệ tiêu đề sách phải trong khoảng từ 0 đến 1");

        RuleFor(x => x.PublisherPercentage)
            .InclusiveBetween(0, 1).WithMessage(isEng
                ? "PublisherPercentage must be between 0 and 1."
                : "Tỉ lệ nhà xuất bản phải trong khoảng 0 đến 1");

        // Kiểm tra tổng của ba giá trị
        RuleFor(x => x.AuthorNamePercentage + x.TitlePercentage + x.PublisherPercentage)
            .Equal(1).WithMessage(isEng
                ? "The total of AuthorNamePercentage, TitlePercentage, and PublisherPercentage must equal 1."
                : "Tổng các tỉ lệ tên tác giả, tiêu đề sách và nhà xuất bản phải bằng 1");
        When(x => x.ConfidenceThreshold.HasValue, () =>
        {
            RuleFor(x => x.ConfidenceThreshold.Value)
                .InclusiveBetween(0, 100).WithMessage(isEng
                    ? "ConfidenceThreshold must be between 0 and 100."
                    : "Ngưỡng tin cậy phải trong khoảng từ 0 đến 100");
        });

        When(x => x.MinFieldThreshold.HasValue, () =>
        {
            RuleFor(x => x.MinFieldThreshold.Value)
                .InclusiveBetween(0, 100).WithMessage(isEng
                    ? "MinFieldThreshold must be between 0 and 100."
                    : "Điểm tin cậy cho giá trị nhỏ nhất phải trong khoảng từ 0 đến 100");
        });
}
}