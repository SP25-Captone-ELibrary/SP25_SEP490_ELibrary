using FluentValidation;
using FPTU_ELibrary.Application.Dtos.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FPTU_ELibrary.Application.Extensions;
using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.Application.Validations
{
    public class AuthenticatedUserDtoValidator : AbstractValidator<AuthenticateUserDto>
	{
        public AuthenticatedUserDtoValidator(string langContext)
        {
	        var langEnum =
		        (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(langContext);
	        var isEng = langEnum == SystemLanguage.English;
	        
	        // Email
	        RuleFor(u => u.Email)
		        .Matches(@"^[\w-\.]+@([\w-]+\.)+[\w-]{2,4}$")
		        .NotNull()
		        .WithMessage(isEng ? "Not valid email address" : "Email không hợp lệ");
			
	        // Password
			RuleFor(u => u.Password)
				.NotNull()
				.Matches(@"^(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$")
				.WithMessage(isEng
					? "Password must be at least 8 characters long, contain at least 1 uppercase letter, " +
					  "1 number, and 1 special character"
					: "Mật khẩu phải dài ít nhất 8 ký tự, chứa ít nhất 1 chữ cái viết hoa, 1 số và 1 ký tự đặc biệt");
		}
    }
}
