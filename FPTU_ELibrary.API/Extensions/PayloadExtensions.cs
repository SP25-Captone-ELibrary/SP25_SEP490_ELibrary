using FPTU_ELibrary.API.Payloads.Requests;
using FPTU_ELibrary.API.Payloads.Requests.Auth;
using FPTU_ELibrary.API.Payloads.Requests.Author;
using FPTU_ELibrary.API.Payloads.Requests.Category;
using FPTU_ELibrary.API.Payloads.Requests.Employee;
using FPTU_ELibrary.API.Payloads.Requests.Fine;
using FPTU_ELibrary.API.Payloads.Requests.LibraryItem;
using FPTU_ELibrary.API.Payloads.Requests.LibraryItemInstance;
using FPTU_ELibrary.API.Payloads.Requests.OCR;
using FPTU_ELibrary.API.Payloads.Requests.Role;
using FPTU_ELibrary.API.Payloads.Requests.User;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Dtos.AIServices;
using FPTU_ELibrary.Application.Dtos.Auth;
using FPTU_ELibrary.Application.Dtos.Authors;
using FPTU_ELibrary.Application.Dtos.Employees;
using FPTU_ELibrary.Application.Dtos.Fine;
using FPTU_ELibrary.Application.Dtos.LibraryItems;
using FPTU_ELibrary.Application.Dtos.Roles;
using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Entities;

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
					TotalUnits	= req.LibraryItemInstances != null && req.LibraryItemInstances.Any() 
						? req.LibraryItemInstances.Count : 0,
					AvailableUnits = 0,
					BorrowedUnits = 0,
					RequestUnits = 0,
					ReservedUnits = 0
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
		public static LibraryItemInstanceDto ToLibraryItemInstanceDto(this CreateItemInstanceRequest req)
		{
			return new LibraryItemInstanceDto()
			{
				Barcode = req.Barcode,
				
				// Add default one history status
				LibraryItemConditionHistories = new List<LibraryItemConditionHistoryDto>()
				{
					new()
					{
						Condition = req.ConditionStatus
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
						Condition = bec.ConditionStatus
					}
				}
			}).ToList();
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
				RoleType = ((Role)req.RoleTypeIdx).ToString()
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
				Description = req.Description
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

		#region AIService

		public static CheckedBookEditionDto ToCheckedBookEditionDto(
			this CheckBookEditionWithImageRequest req)
		{
				return new  CheckedBookEditionDto()
				{
					Title = req.Title,
					Publisher = req.Publisher,
					Authors = req.Authors,
					Image = req.Image
				};
		}

		#endregion
	}
}
