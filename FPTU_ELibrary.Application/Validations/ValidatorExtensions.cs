using FluentValidation;
using FluentValidation.Results;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Dtos.Auth;
using FPTU_ELibrary.Application.Dtos.Employees;
using FPTU_ELibrary.Application.Dtos.Roles;
using Microsoft.AspNetCore.Http;

namespace FPTU_ELibrary.Application.Validations
{
    public static class ValidatorExtensions
	{
		// Define a dictionary to hold validators for each type.
		private static readonly Dictionary<Type, IValidator> Validators = new()
		{
			{ typeof(IFormFile), new ExcelValidator() },
			{ typeof(BookDto), new BookDtoValidator() },
			{ typeof(UserDto), new UserDtoValidator() },
			{ typeof(EmployeeDto), new EmployeeDtoValidator() },
			{ typeof(SystemRoleDto), new SystemRoleDtoValidator() },
			{ typeof(RefreshTokenDto), new RefreshTokenDtoValidator() },
			{ typeof(AuthenticateUserDto), new AuthenticatedUserDtoValidator() },
			{typeof(NotificationDto), new NotificationDtoValidator()},
			{typeof(NotificationRecipientDto), new NotificationRecipientDtoValidator()}
			
			// Add other Validator pairs here.
		};

		public static async Task<ValidationResult?> ValidateAsync<T>(T dto) where T : class
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
