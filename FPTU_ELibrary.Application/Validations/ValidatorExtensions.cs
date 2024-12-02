using FluentValidation;
using FluentValidation.Results;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Dtos.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FPTU_ELibrary.Application.Validations
{
    public static class ValidatorExtensions
	{
		// Define a dictionary to hold validators for each type.
		private static readonly Dictionary<Type, IValidator> Validators = new()
		{
			{ typeof(BookDto), new BookDtoValidator() },
			{ typeof(UserDto), new UserDtoValidator() },
			{ typeof(RefreshTokenDto), new RefreshTokenDtoValidator() },
			{ typeof(AuthenticateUserDto), new AuthenticatedUserDtoValidator() },
			// Add other Validator pairs here.
		};

		public async static Task<ValidationResult?> ValidateAsync<T>(T dto) where T : class
		{
			// Check if a validator exists for the given type.
			if (Validators.TryGetValue(typeof(T), out var validator) && validator is IValidator<T> typedValidator)
			{
				var result = await typedValidator.ValidateAsync(dto);
				return !result.IsValid ? result : null;
			}

			// If no validator is found, throw an exception.
			throw new InvalidOperationException($"No validator found for type {typeof(T).Name}");
		}
	}
}
