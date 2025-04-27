using FluentValidation;
using FPTU_ELibrary.Application.Configurations;
using FPTU_ELibrary.Application.Extensions;
using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.Application.Validations;

public class LibraryScheduleValidator : AbstractValidator<LibrarySchedule>
{
    private readonly bool _isEnglish;

    public LibraryScheduleValidator(string langContext)
    {
        var langEnum = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(langContext);
        _isEnglish = langEnum == SystemLanguage.English;

        // Phải có ít nhất một schedule
        RuleFor(ls => ls.Schedules)
            .NotEmpty()
            .WithMessage(GetMessage("Phải có ít nhất một lịch.", "At least one schedule is required."));

        // Áp dụng DayScheduleValidator cho từng phần tử
        RuleForEach(ls => ls.Schedules)
            .SetValidator(new DayScheduleValidator(langContext));

        // Không được có ngày lặp giữa các schedule
        RuleFor(ls => ls)
            .Custom((ls, ctx) =>
            {
                var allDays = ls.Schedules.SelectMany(s => s.Days).ToList();
                var duplicates = allDays.GroupBy(d => d).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
                if (duplicates.Any())
                {
                    ctx.AddFailure(GetMessage(
                        $"Các ngày sau bị lặp trong nhiều lịch: {string.Join(", ", duplicates)}.",
                        $"These days are duplicated across schedules: {string.Join(", ", duplicates)}."));
                }
            });
    }

    private string GetMessage(string vi, string en) => _isEnglish ? en : vi;
}