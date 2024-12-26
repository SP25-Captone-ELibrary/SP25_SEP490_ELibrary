using FPTU_ELibrary.API.Payloads.Requests;
using FPTU_ELibrary.API.Payloads.Requests.Auth;
using FPTU_ELibrary.API.Payloads.Requests.Author;
using FPTU_ELibrary.API.Payloads.Requests.Book;
using FPTU_ELibrary.API.Payloads.Requests.BookEdition;
using FPTU_ELibrary.API.Payloads.Requests.Employee;
using FPTU_ELibrary.API.Payloads.Requests.Fine;
using FPTU_ELibrary.API.Payloads.Requests.Role;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Dtos.Auth;
using FPTU_ELibrary.Application.Dtos.Authors;
using FPTU_ELibrary.Application.Dtos.BookEditions;
using FPTU_ELibrary.Application.Dtos.Books;
using FPTU_ELibrary.Application.Dtos.Employees;
using FPTU_ELibrary.Application.Dtos.Fine;
using FPTU_ELibrary.Application.Dtos.Roles;
using FPTU_ELibrary.Application.Extensions;
using FPTU_ELibrary.Application.Utils;
using FPTU_ELibrary.Domain.Common.Enums;

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

		#region Book
		// Mapping from typeof(CreateBookRequest) to typeof(BookDto)
		public static BookDto ToBookDto(this CreateBookRequest req)
		{
			return new BookDto()
			{
				Title = req.Title,
				SubTitle = req.SubTitle,
				Summary = req.Summary,
				// Categories
				BookCategories = req.CategoryIds.Any() 
					// Each item -> initialize BookCategoryDto
					? req.CategoryIds.Select(catId => new BookCategoryDto()
					{
						// Assign category
						CategoryId = catId
					}).ToList() 
					// Default 
					: new List<BookCategoryDto>(),
				// Book editions
				BookEditions = req.BookEditions
					.Select(be => be.ToBookEditionDto()).ToList(),
				// Resources
				BookResources = req.BookResources != null && req.BookResources.Any()
					? req.BookResources.Select(br => br.ToBookResourceDto()).ToList()
					: new List<BookResourceDto>()
			};
		}
		
		// Mapping from typeof(CreateBookEditionRequest) to typeof(BookEditionDto)
		public static BookEditionDto ToBookEditionDto(this CreateBookEditionRequest req)
		{
			return new BookEditionDto()
			{
				EditionTitle = req.EditionTitle,
				EditionNumber = req.EditionNumber,
				EditionSummary = req.EditionSummary,
				PageCount = req.PageCount,
				Language = req.Language,
				PublicationYear = req.PublicationYear,
				Format = req.BookFormat,
				CoverImage = req.CoverImage,
				Publisher = req.Publisher,
				Isbn = req.Isbn,
				EstimatedPrice = req.EstimatedPrice,
				// Copies
				BookEditionCopies = req.BookCopies != null && req.BookCopies.Any()
					? req.BookCopies.Select(bc => bc.ToBookEditionCopyDto()).ToList()
					: new List<BookEditionCopyDto>(),
				// Inventory
				BookEditionInventory = new BookEditionInventoryDto()
				{
					TotalCopies	= req.BookCopies != null && req.BookCopies.Any() 
						? req.BookCopies.Count : 0,
					AvailableCopies = 0,
					RequestCopies = 0,
					ReservedCopies = 0
				},
				// Authors
				BookEditionAuthors = req.AuthorIds.Any()
					? req.AuthorIds.Select(id => new BookEditionAuthorDto() { AuthorId = id }).ToList()
					: new List<BookEditionAuthorDto>()
			};
		}
		
		// Mapping from typeof(CreateBookEditionCopyRequest) to typeof(BookEditionCopyDto)
		public static BookEditionCopyDto ToBookEditionCopyDto(this CreateBookEditionCopyRequest req)
		{
			return new BookEditionCopyDto()
			{
				Code = req.Code,
				
				// Add default one history status
				CopyConditionHistories = new List<CopyConditionHistoryDto>()
				{
					new()
					{
						Condition = req.ConditionStatus
					}
				}
			};
		}
		
		// Mapping from typeof(CreateBookResourceRequest) to typeof(BookResourceDto)
		public static BookResourceDto ToBookResourceDto(this CreateBookResourceRequest req, DateTime? createDate = null)
		{
			return new BookResourceDto
			{
				Provider = req.Provider,
				ProviderPublicId = req.ProviderPublicId,
				ResourceType = req.ResourceType,
				ResourceUrl = req.ResourceUrl,
				ResourceSize = req.ResourceSize,
				FileFormat = req.FileFormat,
			};
		}
		
		// Mapping from typeof(UpdateBookRequest) to typeof(BookDto)
		public static BookDto ToBookDto(this UpdateBookRequest req)
			=> new BookDto()
			{
				Title = req.Title,
				SubTitle = req.SubTitle,
				Summary = req.Summary,
				BookCategories = req.CategoryIds.Select(catId => new BookCategoryDto()
				{
					CategoryId = catId
				}).ToList()
			};
		
		#endregion

		#region Book Edition
		// Mapping from typeof(UpdateBookEditionRequest) to typeof(BookEditionDto)
		public static BookEditionDto ToBookEditionDto(this UpdateBookEditionRequest req)
			=> new()
			{
				EditionTitle = req.EditionTitle,
				EditionSummary = req.EditionSummary,
				EditionNumber = req.EditionNumber,
				PageCount = req.PageCount,
				Language = req.Language,
				PublicationYear = req.PublicationYear,
				CoverImage = req.CoverImage,
				Format = req.Format,
				Publisher = req.Publisher,
				Isbn = req.Isbn,
				CanBorrow = req.CanBorrow,
				EstimatedPrice = req.EstimatedPrice,
				ShelfId = req.ShelfId
			};

		#endregion
		
		#region BookResource
		// Mapping from typeof(UpdateBookResourceRequest) to typeof(BookResourceDto)
		public static BookResourceDto ToBookResourceDto(this UpdateBookResourceRequest req)
			=> new()
			{
				Provider = req.Provider,
				ProviderPublicId = req.ProviderPublicId,
				ResourceSize = req.ResourceSize,
				ResourceUrl = req.ResourceUrl,
				FileFormat = req.FileFormat
			};
		#endregion

		#region Book Edition Copy
		// Mapping from typeof(UpdateBookEditionCopyRequest) to typeof(BookEditionCopyDto)
		public static BookEditionCopyDto ToBookEditionCopyDto(this UpdateBookEditionCopyRequest req)
			=> new()
			{
				Status = req.Status 
			};	
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
				UserCode = req.UserCode,
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
				UserCode = req.UserCode,
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
				VietnameseName = req.VietnameseName ?? null!,
				EnglishName = req.EnglishName ?? null!,
				Description = req.Description
			};
		}
		#endregion
		
		#region FinePolicy	
		public static FinePolicyDto ToFinePolicyDto(this CreateFinePolicyRequest req)
		{
			return new FinePolicyDto()
			{
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
				ConditionType = req.ConditionType,
				FineAmountPerDay = req.FineAmountPerDay,
				FixedFineAmount = req.FixedFineAmount,
				Description = req.Description
			};
		}
		
		#endregion
	}
}
