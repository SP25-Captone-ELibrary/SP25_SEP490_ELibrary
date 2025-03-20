using System.Text.Json.Serialization;
using FPTU_ELibrary.Application.Dtos.Auth;
using FPTU_ELibrary.Application.Dtos.Borrows;
using FPTU_ELibrary.Application.Dtos.Notifications;
using FPTU_ELibrary.Application.Dtos.Roles;

namespace FPTU_ELibrary.Application.Dtos.Employees
{
	public class EmployeeDto
	{
		// Key
		public Guid EmployeeId { get; set; }

		// Employee detail information
		public string? EmployeeCode { get; set; }
		public string Email { get; set; } = null!;
		public string? PasswordHash { get; set; } 
		public string FirstName { get; set; } = null!;
		public string LastName { get; set; } = null!;
		public DateTime? Dob { get; set; }
		public string? Phone { get; set; }
		public string? Avatar { get; set; }
		public string? Address { get; set; }
		public string? Gender { get; set; }

		// Employee join, terminate date
		public DateTime? HireDate { get; set; }
		public DateTime? TerminationDate { get; set; }

		// Mark as active or not
		public bool IsActive { get; set; }
		public bool IsDeleted { get; set; }

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
		//public ICollection<BorrowRecord> BorrowRecords { get; set; } = new List<BorrowRecord>();

		//[JsonIgnore]
		//public ICollection<Fine> FineCreateByNavigations { get; set; } = new List<Fine>();

		//public ICollection<BorrowRequest> BorrowRequests { get; set; } = new List<BorrowRequest>();

		// [JsonIgnore]
		// public ICollection<BorrowRecordDetailDto> BorrowRecordDetails { get; set; } = new List<BorrowRecordDetailDto>();
		
		[JsonIgnore]
		public ICollection<NotificationDto> Notifications { get; set; } = new List<NotificationDto>();

		[JsonIgnore]
		public ICollection<RefreshTokenDto> RefreshTokens { get; set; } = new List<RefreshTokenDto>();
	}
	
	public static class EmployeeDtoExtensions
	{
		public static AuthenticateUserDto ToAuthenticateUserDto(this EmployeeDto employeeDto)
		{
			return new AuthenticateUserDto()
			{
				Id = employeeDto.EmployeeId,
				UserCode = employeeDto.EmployeeCode,
				FirstName = employeeDto.FirstName,
				LastName = employeeDto.LastName,
				Email = employeeDto.Email,
				Avatar = employeeDto.Avatar,
				Address = employeeDto.Address,
				Gender = employeeDto.Gender,
				PasswordHash = employeeDto.PasswordHash,
				RoleId = employeeDto.RoleId,
				CreateDate = employeeDto.CreateDate,
				ModifiedDate = employeeDto.ModifiedDate,
				ModifiedBy = employeeDto.ModifiedBy,
				EmailConfirmed = employeeDto.EmailConfirmed,
				PhoneNumberConfirmed = employeeDto.PhoneNumberConfirmed,
				EmailVerificationCode = employeeDto.EmailVerificationCode,
				TwoFactorEnabled = employeeDto.TwoFactorEnabled,
				TwoFactorSecretKey = employeeDto.TwoFactorSecretKey,
				TwoFactorBackupCodes = employeeDto.TwoFactorBackupCodes,
				PhoneVerificationCode = employeeDto.PhoneVerificationCode,
				PhoneVerificationExpiry = employeeDto.PhoneVerificationExpiry,
				IsActive = employeeDto.IsActive,
				IsDeleted = employeeDto.IsDeleted,
				IsEmployee = true
			};
		}
	}
}
