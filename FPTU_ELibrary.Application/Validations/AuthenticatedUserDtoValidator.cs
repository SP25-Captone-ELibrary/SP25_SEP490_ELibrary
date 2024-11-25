using FluentValidation;
using FPTU_ELibrary.Application.Dtos.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FPTU_ELibrary.Application.Validations
{
    public class AuthenticatedUserDtoValidator : AbstractValidator<AuthenticatedUserDto>
	{
        public AuthenticatedUserDtoValidator()
        {
            RuleFor(u => u.Email)
                .EmailAddress()
                .NotNull();

			RuleFor(u => u.Password)
				.Matches(@"^(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$")
				.WithMessage("Password must be at least 8 characters long, contain at least 1 uppercase letter, " +
					"1 number, and 1 special character.");
		}
    }
}
