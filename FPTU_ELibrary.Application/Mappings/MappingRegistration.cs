using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Dtos.Auth;
using FPTU_ELibrary.Application.Dtos.Authors;
using FPTU_ELibrary.Application.Dtos.Borrows;
using FPTU_ELibrary.Application.Dtos.Employees;
using FPTU_ELibrary.Application.Dtos.Locations;
using FPTU_ELibrary.Application.Dtos.Fine;
using FPTU_ELibrary.Application.Dtos.LibraryCard;
using FPTU_ELibrary.Application.Dtos.LibraryItems;
using FPTU_ELibrary.Application.Dtos.Notifications;
using FPTU_ELibrary.Application.Dtos.Payments;
using FPTU_ELibrary.Application.Dtos.Payments.PayOS;
using FPTU_ELibrary.Application.Dtos.Roles;
using FPTU_ELibrary.Application.Dtos.Suppliers;
using FPTU_ELibrary.Application.Dtos.WarehouseTrackings;
using FPTU_ELibrary.Domain.Entities;
using Mapster;

namespace FPTU_ELibrary.Application.Mappings
{
	public class MappingRegistration : IRegister
	{
		public void Register(TypeAdapterConfig config)
		{
			// From [Entity] to [Dto]
			config.NewConfig<Author, AuthorDto>();
			config.NewConfig<BorrowRecord, BorrowRecordDto>();
			config.NewConfig<BorrowRequest, BorrowRequestDto>();
			config.NewConfig<DigitalBorrow, DigitalBorrowDto>();
			config.NewConfig<Employee, EmployeeDto>();
			config.NewConfig<FinePolicy, FinePolicyDto>();
			config.NewConfig<Fine, FineDto>();
			config.NewConfig<LibraryItemReview, LibraryItemReviewDto>();
			config.NewConfig<LibraryItemGroup, LibraryItemGroupDto>();
			config.NewConfig<LibraryItem, LibraryItemDto>();
			config.NewConfig<LibraryItemResource, LibraryItemResourceDto>();
			config.NewConfig<LibraryItemAuthor, LibraryItemAuthorDto>();
			config.NewConfig<LibraryItemInventory, LibraryItemInventoryDto>();
			config.NewConfig<LibraryItemInstance, LibraryItemInstanceDto>();
			config.NewConfig<LibraryItemConditionHistory, LibraryItemConditionHistoryDto>();
			config.NewConfig<LibraryShelf, LibraryShelfDto>();
			config.NewConfig<LibrarySection, LibrarySectionDto>();
			config.NewConfig<LibraryZone, LibraryZoneDto>();
			config.NewConfig<LibraryFloor, LibraryFloorDto>();
			config.NewConfig<LibraryPath, LibraryPathDto>();
			config.NewConfig<LibraryCard, LibraryCardDto>();
			config.NewConfig<LibraryCardPackage, LibraryCardPackageDto>();
			config.NewConfig<RefreshToken, RefreshTokenDto>();
			config.NewConfig<RolePermission, RolePermissionDto>();
			config.NewConfig<ReservationQueue, ReservationQueueDto>();
			config.NewConfig<Supplier, SupplierDto>();
			config.NewConfig<SystemRole, SystemRoleDto>();
			config.NewConfig<SystemMessage, SystemMessageDto>();
			config.NewConfig<SystemFeature, SystemFeatureDto>();
			config.NewConfig<SystemPermission, SystemPermissionDto>();
			config.NewConfig<Notification, NotificationDto>();
			config.NewConfig<NotificationRecipient, NotificationRecipientDto>();
			config.NewConfig<Transaction, TransactionDto>();
			config.NewConfig<PaymentMethod, PaymentMethodDto>();
			config.NewConfig<User, UserDto>();
			config.NewConfig<UserFavorite, UserFavoriteDto>();
			config.NewConfig<WarehouseTracking, WarehouseTrackingDto>();
			config.NewConfig<WarehouseTrackingDetail, WarehouseTrackingDetailDto>();
			
			// From [Dto] to [Entity]
			config.NewConfig<AuthorDto, Author>()
				.Ignore(dest => dest.AuthorId)
				// .Ignore(dest => dest.CreateDate)
				.IgnoreNullValues(true);
			config.NewConfig<CategoryDto, Category>()
				.Ignore(dest => dest.CategoryId)
				.IgnoreNullValues(true);
			config.NewConfig<EmployeeDto, Employee>()
				.Ignore(dest => dest.Role)
				.IgnoreNullValues(false);
			config.NewConfig<FinePolicyDto, FinePolicy>()
				.Ignore(dest => dest.FinePolicyId)
				.IgnoreNullValues(true);
			config.NewConfig<UserDto, User>()
				.Ignore(dest => dest.Role)
				.IgnoreNullValues(false);
			config.NewConfig<TransactionDto, Transaction>()
				.IgnoreNullValues(true);
		}
	}
}
