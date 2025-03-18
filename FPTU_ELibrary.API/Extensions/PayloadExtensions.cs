using FPTU_ELibrary.API.Payloads.Requests;
using FPTU_ELibrary.API.Payloads.Requests.Auth;
using FPTU_ELibrary.API.Payloads.Requests.Author;
using FPTU_ELibrary.API.Payloads.Requests.Borrow;
using FPTU_ELibrary.API.Payloads.Requests.Category;
using FPTU_ELibrary.API.Payloads.Requests.Employee;
using FPTU_ELibrary.API.Payloads.Requests.Fine;
using FPTU_ELibrary.API.Payloads.Requests.LibraryCard;
using FPTU_ELibrary.API.Payloads.Requests.LibraryItem;
using FPTU_ELibrary.API.Payloads.Requests.LibraryItemInstance;
using FPTU_ELibrary.API.Payloads.Requests.Role;
using FPTU_ELibrary.API.Payloads.Requests.Supplier;
using FPTU_ELibrary.API.Payloads.Requests.Transaction;
using FPTU_ELibrary.API.Payloads.Requests.User;
using FPTU_ELibrary.API.Payloads.Requests.WarehouseTracking;
using FPTU_ELibrary.API.Payloads.Requests.WarehouseTrackingDetail;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Dtos.AIServices;
using FPTU_ELibrary.Application.Dtos.Auth;
using FPTU_ELibrary.Application.Dtos.Authors;
using FPTU_ELibrary.Application.Dtos.Borrows;
using FPTU_ELibrary.Application.Dtos.Employees;
using FPTU_ELibrary.Application.Dtos.Fine;
using FPTU_ELibrary.Application.Dtos.LibraryCard;
using FPTU_ELibrary.Application.Dtos.LibraryItems;
using FPTU_ELibrary.Application.Dtos.Payments;
using FPTU_ELibrary.Application.Dtos.Roles;
using FPTU_ELibrary.Application.Dtos.Suppliers;
using FPTU_ELibrary.Application.Dtos.WarehouseTrackings;
using FPTU_ELibrary.Application.Utils;
using FPTU_ELibrary.Domain.Common.Enums;
using Microsoft.AspNetCore.ResponseCompression;

namespace FPTU_ELibrary.API.Extensions
{
	// Summary:
	//		This class provide extensions method mapping from request payload to specific 
	//		application objects
	public static class PayloadExtensions
	{
		#region Auth
		// Mapping from typeof(SignInRequest) to typeof(AuthenticateUserDto)
		public static AuthenticateUserDto ToAuthenticatedUser(this SignInRequest req)
			=> new AuthenticateUserDto
			{
				Email = req.Email,
				Password = null!
			};

		// Mapping from typeof(SignInWithOtpRequest) to typeof(AuthenticateUserDto)
		public static AuthenticateUserDto ToAuthenticatedUser(this SignInWithOtpRequest req)
			=> new AuthenticateUserDto
			{
				Email = req.Email,
				Password = null!
			};

		// Mapping from typeof(SignInWithPasswordRequest) to typeof(AuthenticateUserDto)
		public static AuthenticateUserDto ToAuthenticatedUser(this SignInWithPasswordRequest req)
			=> new AuthenticateUserDto
			{
				Email = req.Email,
				Password = req.Password
			};

		// Mapping from typeof(SignUpRequest) to typeof(AuthenticateUserDto)
		public static AuthenticateUserDto ToAuthenticatedUser(this SignUpRequest req)
			=> new AuthenticateUserDto
			{
				UserCode = req.UserCode,
				Email = req.Email,
				FirstName = req.FirstName,
				LastName = req.LastName,
				Password = req.Password,
				IsEmployee = false
			};
		#endregion

		#region Author

		// Mapping from typeof(CreateAuthorRequest) to typeof(AuthorDto)
		public static AuthorDto ToAuthorDto(this CreateAuthorRequest req)
		{
			// Current local datetime
			var currentLocalDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
				// Vietnam timezone
				TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));

			return new AuthorDto()
			{
				AuthorCode = req.AuthorCode,
				AuthorImage = req.AuthorImage,
				FullName = req.FullName,
				Biography = req.Biography,
				Dob = req.Dob,
				DateOfDeath = req.DateOfDeath,
				Nationality = req.Nationality,
				CreateDate = currentLocalDateTime,
				IsDeleted = false
			};
		}

		// Mapping from typeof(UpdateAuthorRequest) to typeof(AuthorDto)
		public static AuthorDto ToAuthorDto(this UpdateAuthorRequest req)
		{
			// Current local datetime
			var currentLocalDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
				// Vietnam timezone
				TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));

			return new AuthorDto()
			{
				AuthorCode = req.AuthorCode,
				AuthorImage = req.AuthorImage,
				FullName = req.FullName,
				Biography = req.Biography,
				Dob = req.Dob,
				DateOfDeath = req.DateOfDeath,
				Nationality = req.Nationality,
				UpdateDate = currentLocalDateTime,
				IsDeleted = false
			};
		}

		#endregion

		#region Borrow Request
		// Mapping from (CreateBorrowRequest) to typeof(BorrowRequestDto)
		public static BorrowRequestDto ToBorrowRequestDto(this CreateBorrowRequest req)
			=> new()
			{
				Description = req.Description,
				BorrowRequestDetails = req.LibraryItemIds.Select(lId => new BorrowRequestDetailDto()
				{
					LibraryItemId = lId
				}).ToList()
			};
		#endregion

		#region Borrow Record
		// Mapping from (ProcessToBorrowRecordRequest) to typeof(BorrowRecordDto)
		public static BorrowRecordDto ToBorrowRecordDto(this ProcessToBorrowRecordRequest req)
			=> new()
			{
				BorrowRequestId = req.BorrowRequestId,
				LibraryCardId = req.LibraryCardId,
				BorrowType = BorrowType.TakeHome, // Default online process is take home
				BorrowRecordDetails = req.BorrowRecordDetails.Select(brd => new BorrowRecordDetailDto()
				{
					LibraryItemInstanceId = brd.LibraryItemInstanceId
				}).ToList()
			};
		
		// Mapping from (CreateBorrowRecordRequest) to typeof(BorrowRecordDto)
		public static BorrowRecordDto ToBorrowRecordDto(this CreateBorrowRecordRequest req)
			=> new()
			{
				LibraryCardId = req.LibraryCardId,
				BorrowType = req.BorrowType,
				BorrowRecordDetails = req.BorrowRecordDetails.Select(brd => new BorrowRecordDetailDto()
				{
					LibraryItemInstanceId = brd.LibraryItemInstanceId
				}).ToList()
			};
		
		// Mapping from (SelfCheckoutBorrowRequest) to typeof(BorrowRecordDto)
		public static BorrowRecordDto ToBorrowRecordDto(this SelfCheckoutBorrowRequest req)
			=> new()
			{
				LibraryCardId = req.LibraryCardId,
				BorrowType = req.BorrowType,
				BorrowRecordDetails = req.BorrowRecordDetails.Select(brd => new BorrowRecordDetailDto()
				{
					LibraryItemInstanceId = brd.LibraryItemInstanceId
				}).ToList()
			};
		
		// Mapping from (ReturnBorrowRecordRequest) to typeof(BorrowRecordDto)
		public static BorrowRecordDto ToBorrowRecordWithReturnDto(this ReturnBorrowRecordRequest req)
		{
			return new()
			{
				BorrowRecordDetails = req.BorrowRecordDetails.Select(brd => new BorrowRecordDetailDto()
				{
					LibraryItemInstanceId = brd.LibraryItemInstanceId,
					ReturnConditionId = brd.ReturnConditionId,
					ImagePublicIds = brd.ConditionImages != null && brd.ConditionImages.Any() 
						? String.Join(",", brd.ConditionImages)
						: null,
					Fines = brd.Fines.Select(f => new FineDto()
					{
						FinePolicyId = f.FinePolicyId,
						FineNote = f.FineNote
					}).ToList()
				}).ToList()
			};
		}
		// Mapping from (LostBorrowRecordRequest) to typeof(BorrowRecordDto)
		public static BorrowRecordDto ToBorrowRecordWithLostDto(this ReturnBorrowRecordRequest req)
		{
			return new()
			{
				BorrowRecordDetails = req.LostBorrowRecordDetails.Select(brd => new BorrowRecordDetailDto()
				{
					BorrowRecordDetailId = brd.BorrowRecordDetailId,
					Fines = brd.Fines.Select(f => new FineDto()
					{
						FinePolicyId = f.FinePolicyId,
						FineNote = f.FineNote
					}).ToList()
				}).ToList()
			};
		}
		#endregion
		
		#region Library Item

		// Mapping from typeof(CreateLibraryItemRequest) to typeof(LibraryItemDto)
		public static LibraryItemDto ToLibraryItemDto(this CreateLibraryItemRequest req)
		{
			return new LibraryItemDto()
			{
				Title = req.Title,
				SubTitle = req.SubTitle,
				Responsibility = req.Responsibility,
				Edition = req.Edition,
				EditionNumber = req.EditionNumber,
				Language = req.Language,
				OriginLanguage = req.OriginLanguage,
				Summary = req.Summary,
				CoverImage = req.CoverImage,
				PublicationYear = req.PublicationYear,
				Publisher = req.Publisher,
				PublicationPlace = req.PublicationPlace,
				ClassificationNumber = req.ClassificationNumber,
				CutterNumber = req.CutterNumber,
				Isbn = req.Isbn,
				Ean = req.Ean,
				EstimatedPrice = req.EstimatedPrice,
				PageCount = req.PageCount,
				PhysicalDetails = req.PhysicalDetails,
				Dimensions = req.Dimensions,
				AccompanyingMaterial = req.AccompanyingMaterial,
				Genres = req.Genres,
				GeneralNote = req.GeneralNote,
				BibliographicalNote = req.BibliographicalNote,
				TopicalTerms = req.TopicalTerms,
				AdditionalAuthors = req.AdditionalAuthors,
				// In-library management fields
				CategoryId = req.CategoryId,
				GroupId = req.GroupId,
				// Instances
				LibraryItemInstances = req.LibraryItemInstances != null && req.LibraryItemInstances.Any()
					? req.LibraryItemInstances.Select(bc => bc.ToLibraryItemInstanceDto()).ToList()
					: new List<LibraryItemInstanceDto>(),
				// Inventory
				LibraryItemInventory = new LibraryItemInventoryDto()
				{
					TotalUnits = req.LibraryItemInstances != null && req.LibraryItemInstances.Any()
						? req.LibraryItemInstances.Count
						: 0,
					AvailableUnits = 0,
					BorrowedUnits = 0,
					RequestUnits = 0,
					ReservedUnits = 0,
					LostUnits = 0
				},
				// Authors
				LibraryItemAuthors = req.AuthorIds.Any()
					? req.AuthorIds.Select(id => new LibraryItemAuthorDto() { AuthorId = id }).ToList()
					: new List<LibraryItemAuthorDto>(),
				// Resources
				LibraryItemResources = req.LibraryResources != null && req.LibraryResources.Any()
					? req.LibraryResources.Select(lr => new LibraryItemResourceDto()
					{
						LibraryResource = lr.ToLibraryResourceDto()
					}).ToList()
					: new List<LibraryItemResourceDto>()
			};
		}

		// Mapping from typeof(CreateItemInstanceRequest) to typeof(LibraryItemInstanceDto)
		private static LibraryItemInstanceDto ToLibraryItemInstanceDto(this CreateItemInstanceRequest req)
		{
			return new LibraryItemInstanceDto()
			{
				Barcode = req.Barcode,

				// Add default one history status
				LibraryItemConditionHistories = new List<LibraryItemConditionHistoryDto>()
				{
					new()
					{
						ConditionId = req.ConditionId
					}
				}
			};
		}

		// Mapping from typeof(CreateLibraryResourceRequest) to typeof(LibraryResourceDto)
		public static LibraryResourceDto ToLibraryResourceDto(this CreateLibraryResourceRequest req)
		{
			return new LibraryResourceDto
			{
				ResourceTitle = req.ResourceTitle,
				Provider = req.Provider,
				ProviderPublicId = req.ProviderPublicId,
				ResourceType = req.ResourceType,
				ResourceUrl = req.ResourceUrl,
				ResourceSize = req.ResourceSize,
				FileFormat = req.FileFormat,
				DefaultBorrowDurationDays = req.DefaultBorrowDurationDays,
				BorrowPrice = req.BorrowPrice,
				IsDeleted = false
			};
		}

		// Mapping from typeof(UpdateLibraryItemRequest) to typeof(LibraryItemDto)
		public static LibraryItemDto ToLibraryItemDto(this UpdateLibraryItemRequest req)
			=> new()
			{
				Title = req.Title,
				SubTitle = req.SubTitle,
				Responsibility = req.Responsibility,
				Edition = req.Edition,
				EditionNumber = req.EditionNumber,
				Language = req.Language,
				OriginLanguage = req.OriginLanguage,
				Summary = req.Summary,
				CoverImage = req.CoverImage,
				PublicationYear = req.PublicationYear,
				Publisher = req.Publisher,
				PublicationPlace = req.PublicationPlace,
				ClassificationNumber = req.ClassificationNumber,
				CutterNumber = req.CutterNumber,
				Isbn = req.Isbn,
				Ean = req.Ean,
				EstimatedPrice = req.EstimatedPrice,
				PageCount = req.PageCount,
				PhysicalDetails = req.PhysicalDetails,
				Dimensions = req.Dimensions,
				AccompanyingMaterial = req.AccompanyingMaterial,
				Genres = req.Genres,
				GeneralNote = req.GeneralNote,
				BibliographicalNote = req.BibliographicalNote,
				TopicalTerms = req.TopicalTerms,
				AdditionalAuthors = req.AdditionalAuthors,
				// In-library management fields
				CategoryId = req.CategoryId,
				ShelfId = req.ShelfId
			};

		#endregion

		#region Library Item Resource

		// Mapping from typeof(UpdateLibraryResourceRequest) to typeof(LibraryResourceDto)
		public static LibraryResourceDto ToLibraryResourceDto(this UpdateLibraryResourceRequest req)
			=> new()
			{
				ResourceTitle = req.ResourceTitle,
				Provider = req.Provider,
				ProviderPublicId = req.ProviderPublicId,
				ResourceSize = req.ResourceSize,
				ResourceUrl = req.ResourceUrl,
				FileFormat = req.FileFormat,
				DefaultBorrowDurationDays = req.DefaultBorrowDurationDays,
				BorrowPrice = req.BorrowPrice,
			};

		#endregion

		#region Library Item Instance

		// Mapping from typeof(UpdateItemInstanceRequest) to typeof(LibraryItemInstanceDto)
		public static LibraryItemInstanceDto ToLibraryItemInstanceDto(this UpdateItemInstanceRequest req)
			=> new()
			{
				Status = req.Status,
				Barcode = req.Barcode
			};

		// Mapping from typeof(UpdateRangeItemInstanceRequest) to typeof(LibraryItemInstanceDto)
		public static List<LibraryItemInstanceDto> ToListLibraryItemInstanceDto(
			this UpdateRangeItemInstanceRequest req)
			=> req.LibraryItemInstances.Select(r => new LibraryItemInstanceDto()
			{
				LibraryItemInstanceId = r.LibraryItemInstanceId,
				Status = r.Status,
				Barcode = r.Barcode
			}).ToList();

		// Mapping from typeof(CreateRangeItemInstanceRequest) to typeof(LibraryItemInstanceDto)
		public static List<LibraryItemInstanceDto> ToListLibraryItemInstanceDto(this CreateRangeItemInstanceRequest req)
			=> req.LibraryItemInstances.Select(bec => new LibraryItemInstanceDto()
			{
				Barcode = bec.Barcode,
				LibraryItemConditionHistories = new List<LibraryItemConditionHistoryDto>()
				{
					new()
					{
						ConditionId = bec.ConditionId
					}
				}
			}).ToList();

		#endregion

		#region Library Card
		// Mapping from (RegisterLibraryCardOnlineRequest) to typeof(LibraryCardDto)
		public static LibraryCardDto ToUserWithLibraryCardDto(this RegisterLibraryCardOnlineRequest req)
			=> new()
			{
				Avatar = req.Avatar,
				FullName = req.FullName
			};
		
		// Mapping from typeof(CreateLibraryCardHolderRequest) to typeof(UserDto)
		public static UserDto ToLibraryCardHolderDto(this CreateLibraryCardHolderRequest req)
		{
			return new()
			{
				Email = req.Email,
				FirstName = req.FirstName,
				LastName = req.LastName,
				Phone = req.Phone,
				Address = req.Address,
				Gender = req.Gender,
				Dob = req.Dob,
				Avatar = req.Avatar,
				LibraryCard = new()
				{
					FullName = $"{req.FirstName} {req.LastName}",
					Avatar = req.Avatar
				}
			};
		}
		
		// Mapping from typeof(UpdateLibraryCardHolderRequest) to typeof(UserDto)
		public static UserDto ToLibraryCardHolderDto(this UpdateLibraryCardHolderRequest req)
		{
			return new()
			{
				FirstName = req.FirstName,
				LastName = req.LastName,
				Phone = req.Phone,
				Address = req.Address,
				Gender = req.Gender,
				Dob = req.Dob
			};
		}
		
		// Mapping from typeof(UpdateLibraryCardRequest) to typeof(LibraryCardDto)
		public static LibraryCardDto ToLibraryCardDto(this UpdateLibraryCardRequest req)
		{
			return new LibraryCardDto()
			{
				FullName = req.FullName,
				Avatar = req.Avatar,
				IssuanceMethod = req.IssuanceMethod,
				IsAllowBorrowMore = req.IsAllowBorrowMore,
				AllowBorrowMoreReason = req.AllowBorrowMoreReason,
				MaxItemOnceTime = req.MaxItemOnceTime,
				TotalMissedPickUp = req.TotalMissedPickUp
			};
		}
		#endregion
		
		#region Library Card Package
		// Mapping from (CreateLibraryCardPackageRequest) to typeof(LibraryCardPackageDto)
		public static LibraryCardPackageDto ToLibraryCardPackageDto(this CreateLibraryCardPackageRequest req)
		{
			// Current local datetime
			var currentLocalDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
				// Vietnam timezone
				TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));

			return new()
			{
				PackageName = req.PackageName,
				Price = req.Price,
				DurationInMonths = req.DurationInMonths,
				Description = req.Description,
				IsActive = true,
				CreatedAt = currentLocalDateTime
			};
		}

		// Mapping from (UpdateLibraryCardPackageRequest) to typeof(LibraryCardPackageDto)
		public static LibraryCardPackageDto ToLibraryCardPackageDto(this UpdateLibraryCardPackageRequest req)
		{
			return new()
			{
				PackageName = req.PackageName,
				Price = req.Price,
				DurationInMonths = req.DurationInMonths,
				Description = req.Description,
			};
		}
		#endregion
		
		#region User

		// Mapping from typeof(CreateUserRequest) to typeof(UserDto)
		public static UserDto ToUser(this CreateUserRequest req)
		{
			// Current local datetime
			var currentLocalDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
				// Vietnam timezone
				TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));

			return new UserDto
			{
				Email = req.Email,
				FirstName = req.FirstName,
				LastName = req.LastName,
				Dob = req.Dob,
				Phone = req.Phone,
				Address = req.Address,
				Gender = req.Gender.ToString(),

				// Set default authorization values
				CreateDate = currentLocalDateTime,
				IsActive = false,
				EmailConfirmed = false,
				PhoneNumberConfirmed = false,
				TwoFactorEnabled = false
			};
		}

		// Mapping from typeof(UpdateUserRequest) to typeof(UserDto)
		public static UserDto ToUserForUpdate(this UpdateUserRequest req)
		{
			// Current local datetime
			var currentLocalDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
				// Vietnam timezone
				TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));

			return new UserDto
			{
				FirstName = req.FirstName,
				LastName = req.LastName,
				Dob = req.Dob,
				Phone = req.Phone,
				Address = req.Address,
				Gender = req.Gender.ToString(),
				ModifiedDate = currentLocalDateTime
			};
		}
		#endregion

		#region Employee

		// Mapping from typeof(CreateEmployeeRequest) to typeof(EmployeeDto)
		public static EmployeeDto ToEmployeeDtoForCreate(this CreateEmployeeRequest req)
		{
			// Current local datetime
			var currentLocalDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
				// Vietnam timezone
				TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));

			return new EmployeeDto()
			{
				EmployeeCode = req.EmployeeCode,
				Email = req.Email,
				FirstName = req.FirstName,
				LastName = req.LastName,
				Dob = req.Dob,
				Phone = req.Phone,
				Address = req.Address,
				Gender = req.Gender.ToString(),
				HireDate = req.HireDate,
				TerminationDate = req.TerminationDate,
				RoleId = req.RoleId,

				// Set default authorization values
				CreateDate = currentLocalDateTime,
				IsActive = false,
				EmailConfirmed = false,
				PhoneNumberConfirmed = false,
				TwoFactorEnabled = false
			};
		}

		public static EmployeeDto ToEmployeeDtoForUpdate(this UpdateRequest req)
		{
			// Current local datetime
			var currentLocalDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
				// Vietnam timezone
				TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));

			return new EmployeeDto()
			{
				EmployeeCode = req.EmployeeCode,
				FirstName = req.FirstName,
				LastName = req.LastName,
				Dob = req.Dob,
				Phone = req.Phone,
				Address = req.Address,
				Gender = req.Gender.ToString(),
				HireDate = req.HireDate,
				Avatar = req.Avatar,
				TerminationDate = req.TerminationDate,
				ModifiedDate = currentLocalDateTime
			};
		}

		public static EmployeeDto ToEmployeeDtoForUpdateProfile(this UpdateProfileRequest req)
		{
			// Current local datetime
			var currentLocalDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
				// Vietnam timezone
				TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));

			return new EmployeeDto()
			{
				FirstName = req.FirstName,
				LastName = req.LastName,
				Dob = req.Dob,
				Phone = req.Phone,
				Address = req.Address,
				Gender = req.Gender.ToString(),
				ModifiedDate = currentLocalDateTime,
				Avatar = req.Avatar
			};
		}

		#endregion

		#region Role

		public static SystemRoleDto ToSystemRoleDto(this UpdateRoleRequest req, int roleId)
			=> new SystemRoleDto()
			{
				RoleId = roleId,
				EnglishName = req.EnglishName,
				VietnameseName = req.VietnameseName,
				RoleType = req.RoleTypeIdx.ToString()
			};

		#endregion

		#region Category

		public static CategoryDto ToCategoryForUpdate(this UpdateCategoryRequest req)
		{
			return new CategoryDto()
			{
				Prefix = req.Prefix,
				VietnameseName = req.VietnameseName,
				EnglishName = req.EnglishName,
				Description = req.Description,
				IsAllowAITraining = req.IsAllowAITraining,
				TotalBorrowDays = req.TotalBorrowDays
			};
		}

		#endregion

		#region FinePolicy
		public static FinePolicyDto ToFinePolicyDto(this CreateFinePolicyRequest req)
		{
			return new FinePolicyDto()
			{
				FinePolicyTitle = req.FinePolicyTitle,
				ConditionType = req.ConditionType,
				FineAmountPerDay = req.FineAmountPerDay,
				FixedFineAmount = req.FixedFineAmount,
				Description = req.Description
			};
		}

		public static FinePolicyDto ToFinePolicyDto(this UpdateFinePolicyRequest req)
		{
			return new FinePolicyDto()
			{
				FinePolicyTitle = req.FinePolicyTitle,
				ConditionType = req.ConditionType,
				FineAmountPerDay = req.FineAmountPerDay,
				FixedFineAmount = req.FixedFineAmount,
				Description = req.Description
			};
		}

		#endregion

		#region Supplier

		// Mapping from typeof(CreateSupplierRequest) to typeof(SupplierDto)
		public static SupplierDto ToSupplierDto(this CreateSupplierRequest req)
		{
			// Current local datetime
			var currentLocalDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
				// Vietnam timezone
				TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));

			return new()
			{
				SupplierName = req.SupplierName,
				SupplierType = req.SupplierType,
				ContactPerson = req.ContactPerson,
				ContactEmail = req.ContactEmail,
				ContactPhone = req.ContactPhone,
				Address = req.Address,
				Country = req.Country,
				City = req.City,
				IsDeleted = false,
				IsActive = true,
				CreatedAt = currentLocalDateTime
			};
		}

		// Mapping from typeof(UpdateSupplierRequest) to typeof(SupplierDto)
		public static SupplierDto ToSupplierDto(this UpdateSupplierRequest req)
		{
			// Current local datetime
			var currentLocalDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
				// Vietnam timezone
				TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));

			return new()
			{
				SupplierName = req.SupplierName,
				SupplierType = req.SupplierType,
				ContactPerson = req.ContactPerson,
				ContactEmail = req.ContactEmail,
				ContactPhone = req.ContactPhone,
				Address = req.Address,
				Country = req.Country,
				City = req.City,
				IsDeleted = false,
				IsActive = true,
				UpdatedAt = currentLocalDateTime
			};
		}

		#endregion

		#region Warehouse Tracking

		// Mapping from typeof(CreateWarehouseTracking) to typeof(WarehouseTrackingDto)
		public static WarehouseTrackingDto ToWarehouseTrackingDto(this CreateWarehouseTrackingRequest req)
		{
			return new WarehouseTrackingDto()
			{
				SupplierId = req.SupplierId,
				TotalItem = req.TotalItem,
				TotalAmount = req.TotalAmount,
				TrackingType = req.TrackingType,
				TransferLocation = req.TransferLocation,
				Description = req.Description,
				EntryDate = req.EntryDate,
				ExpectedReturnDate = req.ExpectedReturnDate,
				// Default inventory values
				WarehouseTrackingInventory = new ()
				{
					TotalItem = 0,
					TotalCatalogedItem = 0,
					TotalInstanceItem = 0,
					TotalCatalogedInstanceItem = 0
				}
			};
		}

		// Mapping from typeof(UpdateWarehouseTrackingRequest) to typeof(WarehouseTrackingDto)
		public static WarehouseTrackingDto ToWarehouseTrackingDto(this UpdateWarehouseTrackingRequest req)
		{
			return new WarehouseTrackingDto()
			{
				SupplierId = req.SupplierId,
				TotalItem = req.TotalItem,
				TotalAmount = req.TotalAmount,
				TrackingType = req.TrackingType,
				TransferLocation = req.TransferLocation,
				Description = req.Description,
				EntryDate = req.EntryDate,
				ExpectedReturnDate = req.ExpectedReturnDate
			};
		}
		
		// Mapping from typeof(CreateStockInRequest) to typeof(WarehouseTrackingDto)
		public static WarehouseTrackingDto ToWarehouseTrackingDto(this CreateStockInRequest req)
		{
			// Total item <- Number of warehouse tracking details  
			var totalItem = req.WarehouseTrackingDetails.Count;
			// Total instance item <- Sum of each warehouse tracking detail's item total
			var totalInstanceItem = req.WarehouseTrackingDetails.Count != 0
                ? req.WarehouseTrackingDetails
					.Select(wtd => wtd.ItemTotal).Sum() 
				: 0;
			// Total cataloged item <- Any tracking detail request along with libraryItemId > 0 or libraryItem != null
			var totalCatalogedItem = req.WarehouseTrackingDetails
				.Count(wtd =>
					(wtd.LibraryItemId != null && wtd.LibraryItemId > 0) || wtd.LibraryItem != null);
			// Total instance cataloged item  
			// var totalCatalogedInstanceItem = req.WarehouseTrackingDetails
			// 	.Where(wtd => wtd.LibraryItemId != null && wtd.LibraryItemId != 0)
			// 	.Select(wtd => wtd.ItemTotal).Sum();
			// Sum of number of library item instances in each warehouse tracking detail's item
			// var totalItemToBeCataloged = req.WarehouseTrackingDetails.Count != 0
			// 	? req.WarehouseTrackingDetails
			// 		.Where(wtd => wtd.LibraryItem != null)
			// 		.Select(wtd => wtd.LibraryItem?.LibraryItemInstances?.Count)
			// 		.Sum()
			// 	: 0;
			
            return new()
            {
                SupplierId = req.SupplierId,
                Description = req.Description,
                EntryDate = req.EntryDate,
                TotalItem = req.TotalItem,
                TotalAmount = req.TotalAmount,
                WarehouseTrackingDetails = req.WarehouseTrackingDetails.Any()
                    ? req.WarehouseTrackingDetails.Select(wtd => wtd.ToWarehouseTrackingDetailDto()).ToList()
                    : new(),
                // Inventory
                WarehouseTrackingInventory = new WarehouseTrackingInventoryDto()
                {
	                TotalItem = totalItem,
					TotalInstanceItem = totalInstanceItem,
	                TotalCatalogedItem = totalCatalogedItem,
	                TotalCatalogedInstanceItem = 0
                }
            };
        }
		
		// Mapping from typeof(CreateStockInDetailRequest) to typeof(WarehouseTrackingDetailDto)
		private static WarehouseTrackingDetailDto ToWarehouseTrackingDetailDto(this CreateStockInDetailRequest req)
		{
			return new()
			{
				ItemName = req.ItemName,
				ItemTotal = req.ItemTotal,
				Isbn = !string.IsNullOrEmpty(req.Isbn) ? ISBN.CleanIsbn(req.Isbn) : null,
				UnitPrice = req.UnitPrice,
				TotalAmount = req.TotalAmount,
				CategoryId = req.CategoryId,
				ConditionId = req.ConditionId,
				StockTransactionType = req.StockTransactionType,
				LibraryItemId = req.LibraryItemId,
				LibraryItem = req.LibraryItem?.ToLibraryItemDto()
			};
		}
		
		// Mapping from typeof(CreateStockInDetailItemRequest) to typeof(LibraryItemDto)
		private static LibraryItemDto ToLibraryItemDto(this CreateStockInDetailItemRequest req)
		{
			return new()
			{
				Title = req.Title,
				SubTitle = req.SubTitle,
				Responsibility = req.Responsibility,
				Edition = req.Edition,
				EditionNumber = req.EditionNumber,
				Language = req.Language,
				OriginLanguage = req.OriginLanguage,
				Summary = req.Summary,
				CoverImage = req.CoverImage,
				PublicationYear = req.PublicationYear,
				Publisher = req.Publisher,
				PublicationPlace = req.PublicationPlace,
				ClassificationNumber = req.ClassificationNumber,
				CutterNumber = req.CutterNumber,
				Isbn = req.Isbn != null ? ISBN.CleanIsbn(req.Isbn) : req.Isbn,
				Ean = req.Ean,
				EstimatedPrice = req.EstimatedPrice,
				PageCount = req.PageCount,
				PhysicalDetails = req.PhysicalDetails,
				Dimensions = req.Dimensions,
				AccompanyingMaterial = req.AccompanyingMaterial,
				Genres = req.Genres,
				GeneralNote = req.GeneralNote,
				BibliographicalNote = req.BibliographicalNote,
				TopicalTerms = req.TopicalTerms,
				AdditionalAuthors = req.AdditionalAuthors,
				// In-library management fields
				CategoryId = req.CategoryId,
				// Inventory
				LibraryItemInventory = new LibraryItemInventoryDto()
				{
					TotalUnits = 0,
					AvailableUnits = 0,
					BorrowedUnits = 0,
					RequestUnits = 0,
					ReservedUnits = 0,
					LostUnits = 0
				},
				// Authors
				LibraryItemAuthors = req.AuthorIds.Any()
					? req.AuthorIds.Select(id => new LibraryItemAuthorDto() { AuthorId = id }).ToList()
					: new List<LibraryItemAuthorDto>()
			};
		}
		#endregion

		#region Warehouse Traking Detail
		// Mapping from (CreateWarehouseTrackingDetailRequest) to typeof(WarehouseTrackingDetailDto)
		public static WarehouseTrackingDetailDto ToWarehouseTrackingDetailDto(
			this CreateWarehouseTrackingDetailRequest req)
		{
			return new()
			{
				ItemName = req.ItemName,
				ItemTotal = req.ItemTotal,
				Isbn = req.Isbn != null ? ISBN.CleanIsbn(req.Isbn) : null,
				UnitPrice = req.UnitPrice,
				TotalAmount = req.TotalAmount,
				StockTransactionType = req.StockTransactionType,
				CategoryId = req.CategoryId,
				ConditionId = req.ConditionId,
				LibraryItemId = req.LibraryItemId,
				LibraryItem = req.LibraryItem?.ToLibraryItemDto()
			};
		}
		
		// Mapping from (UpdateWarehouseTrackingDetailRequest) to typeof(WarehouseTrackingDetailDto)
		public static WarehouseTrackingDetailDto ToWarehouseTrackingDetailDto(
			this UpdateWarehouseTrackingDetailRequest req)
		{
			return new()
			{
				ItemName = req.ItemName,
				ItemTotal = req.ItemTotal,
				Isbn = req.Isbn != null ? ISBN.CleanIsbn(req.Isbn) : null,
				UnitPrice = req.UnitPrice,
				TotalAmount = req.TotalAmount,
				StockTransactionType = req.StockTransactionType,
				CategoryId = req.CategoryId,
				ConditionId = req.ConditionId
			};
		}
		#endregion

		#region Transaction
		// Mapping from typeof(CreateTransactionRequest) to typeof(TransactionDto)
		public static TransactionDto ToTransactionDto(this CreateTransactionRequest req)
		{
			return new()
			{
				ResourceId = req.ResourceId,
				LibraryCardPackageId = req.LibraryCardPackageId,
				Description = req.Description,
				PaymentMethodId = req.PaymentMethodId,
				TransactionType = req.TransactionType
			};
		}
		

		#endregion
		
		#region AIService

		// public static CheckedBookEditionDto ToCheckedBookEditionDto(
		// 	this CheckBookEditionWithImageRequest req)
		// {
		// 		return new  CheckedBookEditionDto()
		// 		{
		// 			Title = req.Title,
		// 			Publisher = req.Publisher,
		// 			Authors = req.Authors,
		// 			Image = req.Image
		// 		};
		// }
		public static CheckedItemDto ToCheckedItemDto(this CheckItemWithImagesRequest req)
		{
			return new CheckedItemDto()
			{
				Title = req.Title,
				SubTitle = req.SubTitle,
				GeneralNote = req.GeneralNote,
				Publisher = req.Publisher,
				Authors = req.Authors,
				Images = req.Images
			};
		}

		#endregion
	}
}
