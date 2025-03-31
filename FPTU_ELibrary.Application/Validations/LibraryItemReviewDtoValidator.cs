using FluentValidation;
using FPTU_ELibrary.Application.Dtos.LibraryItems;
using FPTU_ELibrary.Application.Extensions;
using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.Application.Validations;

public class LibraryItemReviewDtoValidator : AbstractValidator<LibraryItemReviewDto>
{
    public LibraryItemReviewDtoValidator(string langContext)
    {
        var langEnum =
            (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(langContext);
        var isEng = langEnum == SystemLanguage.English;
        
        // ReviewText 
        RuleFor(e => e.ReviewText)
            .MaximumLength(1000)
            .WithMessage(e => isEng
                ? "Review text must not exceed 1000 characters"
                : "Đánh giá không vượt quá 1000 ký tự");
        // RatingValue
        RuleFor(e => e.RatingValue)
            .InclusiveBetween(1, 5)
            .WithMessage(e => 
            {
                if (e.RatingValue < 1)
                {
                    return isEng
                        ? "Rating must at least 1 star"
                        : "Đánh giá phải ít nhất 1 sao";
                }
                else if (e.RatingValue > 5)
                {
                    return isEng
                        ? "Rating must smaller than 5 stars"
                        : "Đánh giá chỉ được tối đa 5 sao";
                }
        
                // Default message
                return isEng
                    ? "Invalid rating value"
                    : "Đánh giá không hợp lệ";
            });
    }
}