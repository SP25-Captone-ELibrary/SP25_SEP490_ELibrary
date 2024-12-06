using FPTU_ELibrary.Domain.Entities;
using System.Text.Json.Serialization;
using FPTU_ELibrary.Application.Dtos.Auth;

namespace FPTU_ELibrary.Application.Dtos
{
	public class EmployeeDto
	{
		// Key
		public Guid EmployeeId { get; set; }

		// Employee detail information
		public string? EmployeeCode { get; set; }
		public string Email { get; set; } = null!;
		public string? PasswordHash { get; set; } = null!;
		public string FirstName { get; set; } = null!;
		public string LastName { get; set; } = null!;
		public DateTime? Dob { get; set; }
		public string? Phone { get; set; }
		public string? Avatar { get; set; }
		public string? Address { get; set; }
		public string? Gender { get; set; }

		// Employee join, terminate date
		public DateTime HireDate { get; set; }
		public DateTime? TerminationDate { get; set; }

		// Mark as active or not
		public bool IsActive { get; set; }


		// Creation and modify date
		public DateTime CreateDate { get; set; }
		public DateTime? ModifiedDate { get; set; }
		public string? ModifiedBy { get; set; }
		
		// Multi-factor authentication properties
		public bool TwoFactorEnabled { get; set; }
		public bool PhoneNumberConfirmed { get; set; }
		public bool EmailConfirmed { get; set; }
		public string? TwoFactorSecretKey { get; set; }
		public string? TwoFactorBackupCodes { get; set; }
		public string? PhoneVerificationCode { get; set; }
		public string? EmailVerificationCode { get; set; }
		public DateTime? PhoneVerificationExpiry { get; set; }

		// Role in the system
		public int RoleId { get; set; }

		// Mapping entities
		public SystemRoleDto Role { get; set; } = null!;

		//[JsonIgnore]
		//public ICollection<Book> BookCreateByNavigations { get; set; } = new List<Book>();

		//[JsonIgnore]
		//public ICollection<Book> BookUpdatedByNavigations { get; set; } = new List<Book>();

		//[JsonIgnore]
		//public ICollection<BookEdition> BookEditions { get; set; } = new List<BookEdition>();

		//[JsonIgnore]
		//public ICollection<BookResource> BookResources { get; set; } = new List<BookResource>();

		//[JsonIgnore]
		//public ICollection<BorrowRecord> BorrowRecords { get; set; } = new List<BorrowRecord>();

		//[JsonIgnore]
		//public ICollection<CopyConditionHistory> CopyConditionHistories { get; set; } = new List<CopyConditionHistory>();

		//[JsonIgnore]
		//public ICollection<Fine> FineCreateByNavigations { get; set; } = new List<Fine>();

		//[JsonIgnore]
		//public ICollection<LearningMaterial> LearningMaterialCreateByNavigations { get; set; } = new List<LearningMaterial>();

		//public ICollection<LearningMaterial> LearningMaterialUpdatedByNavigations { get; set; } = new List<LearningMaterial>();
		//public ICollection<BorrowRequest> BorrowRequests { get; set; } = new List<BorrowRequest>();

		//[JsonIgnore]
		//public ICollection<Notification> Notifications { get; set; } = new List<Notification>();

		[JsonIgnore]
		public ICollection<RefreshTokenDto> RefreshTokens { get; set; } = new List<RefreshTokenDto>();
	}
	
	public static class EmployeeDtoExtensions
	{
		public static AuthenticateUserDto ToAuthenticateUserDto(this EmployeeDto userDto)
		{
			return new AuthenticateUserDto()
			{
				Id = userDto.EmployeeId,
				UserCode = userDto.EmployeeCode,
				FirstName = userDto.FirstName,
				LastName = userDto.LastName,
				Email = userDto.Email,
				Avatar = userDto.Avatar,
				Address = userDto.Address,
				Gender = userDto.Gender,
				PasswordHash = userDto.PasswordHash,
				RoleId = userDto.RoleId,
				CreateDate = userDto.CreateDate,
				ModifiedDate = userDto.ModifiedDate,
				ModifiedBy = userDto.ModifiedBy,
				EmailConfirmed = userDto.EmailConfirmed,
				PhoneNumberConfirmed = userDto.PhoneNumberConfirmed,
				EmailVerificationCode = userDto.EmailVerificationCode,
				TwoFactorEnabled = userDto.TwoFactorEnabled,
				TwoFactorSecretKey = userDto.TwoFactorSecretKey,
				TwoFactorBackupCodes = userDto.TwoFactorBackupCodes,
				PhoneVerificationCode = userDto.PhoneVerificationCode,
				PhoneVerificationExpiry = userDto.PhoneVerificationExpiry,
				IsActive = userDto.IsActive,
				IsEmployee = true
			};
		}
	}
}
