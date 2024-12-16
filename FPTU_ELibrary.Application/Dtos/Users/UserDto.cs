using FPTU_ELibrary.Domain.Entities;
using System.Text.Json.Serialization;
using FPTU_ELibrary.Application.Dtos.Auth;
using FPTU_ELibrary.Application.Dtos.Roles;

namespace FPTU_ELibrary.Application.Dtos
{
	public class UserDto
	{
		// Key
		public Guid UserId { get; set; }

		// User detail and credentials information
		public string? UserCode { get; set; }
		public string Email { get; set; } = null!;
		public string? PasswordHash { get; set; } 
		public string? FirstName { get; set; } 
		public string? LastName { get; set; } 
		public DateTime? Dob { get; set; }
		public string? Phone { get; set; }
		public string? Avatar { get; set; }
		public string? Address { get; set; }
		public string? Gender { get; set; }

		// Mark as active user or not 
		public bool IsActive { get; set; }
		public bool IsDeleted { get; set; }
		
		// Creation datetime
		public DateTime CreateDate { get; set; }
		public DateTime? ModifiedDate { get; set; }
		public string? ModifiedBy { get; set; }

		// Multi-factor authentication
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
		//public ICollection<BorrowRecord> BorrowRecords { get; set; } = new List<BorrowRecord>();
		//public ICollection<BorrowRequest> BorrowRequests { get; set; } = new List<BorrowRequest>();
		//public ICollection<ReservationQueue> ReservationQueues { get; set; } = new List<ReservationQueue>();
		//public ICollection<NotificationRecipient> NotificationRecipients { get; set; } = new List<NotificationRecipient>();

		[JsonIgnore]
		public ICollection<RefreshTokenDto> RefreshTokens { get; set; } = new List<RefreshTokenDto>();

		//[JsonIgnore]
		//public ICollection<BookReview> BookReviews { get; set; } = new List<BookReview>();

		//[JsonIgnore]
		//public ICollection<UserFavorite> UserFavorites { get; set; } = new List<UserFavorite>();
	}

	public static class UserDtoExtensions
	{
		public static AuthenticateUserDto ToAuthenticateUserDto(this UserDto userDto)
		{
			return new AuthenticateUserDto()
			{
				Id = userDto.UserId,
				UserCode = userDto.UserCode,
				FirstName = userDto.FirstName ?? null!,
				LastName = userDto.LastName ?? null!,
				Email = userDto.Email,
				Avatar = userDto.Avatar,
				Address = userDto.Address,
				Gender = userDto.Gender,
				Dob = userDto.Dob,
				Phone = userDto.Phone,
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
				IsDeleted = userDto.IsDeleted,
				IsEmployee = false
			};
		}
	}
}
