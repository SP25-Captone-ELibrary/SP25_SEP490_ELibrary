using FluentValidation;
using FluentValidation.Results;
using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Dtos.Auth;
using FPTU_ELibrary.Application.Dtos.Authors;
using FPTU_ELibrary.Application.Dtos.Books;
using FPTU_ELibrary.Application.Dtos.Employees;
using FPTU_ELibrary.Application.Dtos.Notifications;
using FPTU_ELibrary.Application.Dtos.Roles;
using Microsoft.AspNetCore.Http;

namespace FPTU_ELibrary.Application.Validations
{
    public static class ValidatorExtensions
	{
		
		private static IValidator<T>? GetValidator<T>(string language) where T : class
		{
			return typeof(T) switch
			{
				// Create instances of the validators, passing the language 
				{ } when typeof(T) == typeof(IFormFile) => (IValidator<T>)new ExcelValidator(language),
				{ } when typeof(T) == typeof(AuthorDto) => (IValidator<T>)new AuthorDtoValidator(language),
				{ } when typeof(T) == typeof(BookDto) => (IValidator<T>)new BookDtoValidator(),
				{ } when typeof(T) == typeof(UserDto) => (IValidator<T>)new UserDtoValidator(language),
				{ } when typeof(T) == typeof(EmployeeDto) => (IValidator<T>)new EmployeeDtoValidator(language),
				{ } when typeof(T) == typeof(SystemRoleDto) => (IValidator<T>)new SystemRoleDtoValidator(),
				{ } when typeof(T) == typeof(RefreshTokenDto) => (IValidator<T>)new RefreshTokenDtoValidator(),
				{ } when typeof(T) == typeof(AuthenticateUserDto) => (IValidator<T>)new AuthenticatedUserDtoValidator(language),
				{ } when typeof(T) == typeof(NotificationDto) => (IValidator<T>)new NotificationDtoValidator(language),
				{ } when typeof(T) == typeof(NotificationRecipientDto) => (IValidator<T>)new NotificationRecipientDtoValidator(language),
				{ } when typeof(T) == typeof(BookCategoryDto) => (IValidator<T>)new BookCategoryDtoValidator(language),
				_ => null
			};
		}
		
		public static async Task<ValidationResult?> ValidateAsync<T>(T dto) where T : class
		{
			// Retrieve current language
			var currentLanguage = LanguageContext.CurrentLanguage;
			
			// Create a new validator instance for the given type, passing the language dynamically.
			var validator = GetValidator<T>(currentLanguage);
			
			// Check if a validator exists for the given type.
			if (validator != null)
			{
				var result = await validator.ValidateAsync(dto);
				return !result.IsValid ? result : null;
			}

			// If no validator is found, throw an exception.
			throw new InvalidOperationException($"No validator found for type {typeof(T).Name}");
		}
	}
}
