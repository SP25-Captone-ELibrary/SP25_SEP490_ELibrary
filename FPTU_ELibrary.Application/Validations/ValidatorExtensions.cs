using FluentValidation;
using FluentValidation.Results;
using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Configurations;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Dtos.AdminConfiguration;
using FPTU_ELibrary.Application.Dtos.AIServices;
using FPTU_ELibrary.Application.Dtos.Auth;
using FPTU_ELibrary.Application.Dtos.Authors;
using FPTU_ELibrary.Application.Dtos.Borrows;
using FPTU_ELibrary.Application.Dtos.Employees;
using FPTU_ELibrary.Application.Dtos.Fine;
using FPTU_ELibrary.Application.Dtos.LibraryCard;
using FPTU_ELibrary.Application.Dtos.LibraryItems;
using FPTU_ELibrary.Application.Dtos.Notifications;
using FPTU_ELibrary.Application.Dtos.Payments;
using FPTU_ELibrary.Application.Dtos.Roles;
using FPTU_ELibrary.Application.Dtos.Suppliers;
using FPTU_ELibrary.Application.Dtos.WarehouseTrackings;
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
				{ } when typeof(T) == typeof(BorrowRequestDto) => (IValidator<T>)new BorrowRequestDtoValidator(language),
				{ } when typeof(T) == typeof(LibraryItemDto) => (IValidator<T>)new LibraryItemDtoValidator(language),
				{ } when typeof(T) == typeof(LibraryResourceDto) => (IValidator<T>)new LibraryResourceDtoValidator(language),
				{ } when typeof(T) == typeof(LibraryItemInstanceDto) => (IValidator<T>)new LibraryItemInstanceDtoValidator(language),
				{ } when typeof(T) == typeof(LibraryItemInventoryDto) => (IValidator<T>)new LibraryItemInventoryDtoValidator(),
				{ } when typeof(T) == typeof(LibraryItemConditionDto) => (IValidator<T>)new LibraryItemConditionDtoValidator(language),
				{ } when typeof(T) == typeof(LibraryItemReviewDto) => (IValidator<T>)new LibraryItemReviewDtoValidator(language),
				{ } when typeof(T) == typeof(LibraryCardDto) => (IValidator<T>)new LibraryCardDtoValidator(language),
				{ } when typeof(T) == typeof(LibraryCardPackageDto) => (IValidator<T>)new LibraryCardPackageDtoValidator(language),
				{ } when typeof(T) == typeof(LibraryClosureDayDto) => (IValidator<T>)new LibraryClosureDayDtoValidator(language),
				{ } when typeof(T) == typeof(UserDto) => (IValidator<T>)new UserDtoValidator(language),
				{ } when typeof(T) == typeof(EmployeeDto) => (IValidator<T>)new EmployeeDtoValidator(language),
				{ } when typeof(T) == typeof(SupplierDto) => (IValidator<T>)new SupplierDtoValidator(language),
				{ } when typeof(T) == typeof(SupplementRequestDetailDto) => (IValidator<T>)new SupplementRequestDetailDtoValidator(language),
				{ } when typeof(T) == typeof(SystemRoleDto) => (IValidator<T>)new SystemRoleDtoValidator(),
				{ } when typeof(T) == typeof(RefreshTokenDto) => (IValidator<T>)new RefreshTokenDtoValidator(),
				{ } when typeof(T) == typeof(AuthenticateUserDto) => (IValidator<T>)new AuthenticatedUserDtoValidator(language),
				{ } when typeof(T) == typeof(NotificationDto) => (IValidator<T>)new NotificationDtoValidator(language),
				{ } when typeof(T) == typeof(NotificationRecipientDto) => (IValidator<T>)new NotificationRecipientDtoValidator(language),
				{ } when typeof(T) == typeof(CategoryDto) => (IValidator<T>)new CategoryDtoValidator(language),
				{ } when typeof(T) == typeof(FinePolicyDto) => (IValidator<T>)new FinePolicyDtoValidator(language),
				{ } when typeof(T) == typeof(WarehouseTrackingDto) => (IValidator<T>)new WarehouseTrackingDtoValidator(language),
				{ } when typeof(T) == typeof(WarehouseTrackingDetailDto) => (IValidator<T>)new WarehouseTrackingDetailDtoValidator(language),
				{ } when typeof(T) == typeof(LibraryItemGroupDto) => (IValidator<T>)new LibraryItemGroupDtoValidator(language),
				{ } when typeof(T) == typeof(TransactionDto) => (IValidator<T>)new TransactionDtoValidator(language),
				{ } when typeof(T) == typeof(UpdateKeyVaultDto) => (IValidator<T>)new UpdateKeyVaultDtoValidator(language),
				{ } when typeof(T) == typeof(AISettingsDto) => (IValidator<T>)new AISettingsDtoValidator(language),
				{ } when typeof(T) == typeof(AITrainingSessionDto) => (IValidator<T>)new AITrainingSessionDtoValidator(),
				{ } when typeof(T) == typeof(AITrainingDetailDto) => (IValidator<T>)new AITrainingDetailDtoValidator(),
				{ } when typeof(T) == typeof(AITrainingImageDto) => (IValidator<T>)new AITrainingImageDtoValidator(),
				{ } when typeof(T) == typeof(LibrarySchedule) => (IValidator<T>)new LibraryScheduleValidator(language),
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
