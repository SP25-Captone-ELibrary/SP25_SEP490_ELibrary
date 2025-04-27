using FluentValidation;
using FPTU_ELibrary.Application.Configurations;
using FPTU_ELibrary.Application.Extensions;
using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.Application.Validations;

public class DayScheduleValidator : AbstractValidator<DaySchedule>
{
    private readonly bool _isEnglish;

    public DayScheduleValidator(string langContext)
    {
        var langEnum = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(langContext);
        _isEnglish = langEnum == SystemLanguage.English;

        // Days không được để trống, hợp lệ, không trùng
        RuleFor(ds => ds.Days)
            .NotEmpty()
            .WithMessage(GetMessage("Days không được để trống.", "Days cannot be empty."))
            .Must(days => days.All(d => Enum.IsDefined(typeof(DayOfWeek), d)))
            .WithMessage(GetMessage("Các giá trị trong Days phải là một trong các ngày trong tuần.",
                "All values in Days must be valid weekdays."))
            .Must(days => days.Distinct().Count() == days.Count)
            .WithMessage(GetMessage("Không được có ngày trùng lặp trong cùng một lịch.",
                "Days must not contain duplicates within the same schedule."));

        // Open phải là TimeSpan hợp lệ
        RuleFor(ds => ds.Open)
            .Must(t => t >= TimeSpan.Zero)
            .WithMessage(GetMessage("Open phải có định dạng HH:mm:ss.", "Open must be in HH:mm:ss format."));

        // Close phải là TimeSpan hợp lệ
        RuleFor(ds => ds.Close)
            .Must(t => t >= TimeSpan.Zero)
            .WithMessage(GetMessage("Close phải có định dạng HH:mm:ss.", "Close must be in HH:mm:ss format."));

        // Open < Close
        RuleFor(ds => ds)
            .Must(ds => ds.Open < ds.Close)
            .WithMessage(ds => GetMessage(
                $"Thời gian mở ({ds.Open}) phải trước thời gian đóng ({ds.Close}).",
                $"Open time ({ds.Open}) must be before Close time ({ds.Close})."));
    }

    private string GetMessage(string vi, string en) => _isEnglish ? en : vi;
}