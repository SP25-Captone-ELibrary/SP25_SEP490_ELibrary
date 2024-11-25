using FluentValidation;
using FPTU_ELibrary.Application.Dtos;

namespace FPTU_ELibrary.Application.Validations
{
	public class RefreshTokenDtoValidator : AbstractValidator<RefreshTokenDto>
	{
        public RefreshTokenDtoValidator()
        {
			RuleFor(rft => rft)
				.Must(HasValidUserOrEmployeeId)
				.WithMessage("Either UserId or EmployeeId must be provided, but not both.");

			// You can also add validations for other properties here, like:
			RuleFor(rft => rft.CreateDate)
				.LessThan(rft => rft.ExpiryDate)
				.WithMessage("CreateDate must be earlier than ExpiryDate.");
		}

		private bool HasValidUserOrEmployeeId(RefreshTokenDto rfToken)
		{
			// Valid if either UserId or EmployeeId is provided, but not both
			return (rfToken.UserId != null && rfToken.EmployeeId == null) ||
				   (rfToken.UserId == null && rfToken.EmployeeId != null);
		}
	}
}
