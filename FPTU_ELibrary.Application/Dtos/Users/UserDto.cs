using System.Text.Json.Serialization;
using FPTU_ELibrary.Application.Dtos.Auth;
using FPTU_ELibrary.Application.Dtos.Borrows;
using FPTU_ELibrary.Application.Dtos.LibraryCard;
using FPTU_ELibrary.Application.Dtos.LibraryItems;
using FPTU_ELibrary.Application.Dtos.Notifications;
using FPTU_ELibrary.Application.Dtos.Payments;
using FPTU_ELibrary.Application.Dtos.Roles;

namespace FPTU_ELibrary.Application.Dtos
{
	public class UserDto
	{
		// Key
		public Guid UserId { get; set; }
    
		// Role in the system
		public int RoleId { get; set; }
    
		// Library card information
		public Guid? LibraryCardId { get; set; }
    
		// Basic user information
        public string Email { get; set; } = null!;
        public string? FirstName { get; set; } 
        public string? LastName { get; set; } 
        public string? PasswordHash { get; set; }
        public string? Phone { get; set; }
        public string? Avatar { get; set; }
        public string? Address { get; set; }
        public string? Gender { get; set; }
        public DateTime? Dob { get; set; }
    
        // Mark as active or not
        public bool IsActive { get; set; }
    
        public bool IsDeleted { get; set; }
        
        // Check whether user is created from employee or not
        public bool IsEmployeeCreated { get; set; }
        
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
		
		// Mapping entities
		public SystemRoleDto Role { get; set; } = null!;
		public LibraryCardDto? LibraryCard { get; set; } 
		
		public ICollection<NotificationRecipientDto> NotificationRecipients { get; set; } = new List<NotificationRecipientDto>();

		[JsonIgnore]
		public ICollection<TransactionDto> Transactions { get; set; } = new List<TransactionDto>();
    
		[JsonIgnore]
		public ICollection<DigitalBorrowDto> DigitalBorrows { get; set; } = new List<DigitalBorrowDto>();
    
		[JsonIgnore]
		public ICollection<RefreshTokenDto> RefreshTokens { get; set; } = new List<RefreshTokenDto>();

		[JsonIgnore]
		public ICollection<LibraryItemReviewDto> LibraryItemReviews { get; set; } = new List<LibraryItemReviewDto>();

		[JsonIgnore]
		public ICollection<UserFavoriteDto> UserFavorites { get; set; } = new List<UserFavoriteDto>();
	}

	public static class UserDtoExtensions
	{
		public static AuthenticateUserDto ToAuthenticateUserDto(this UserDto userDto)
		{
			return new AuthenticateUserDto()
			{
				Id = userDto.UserId,
				LibraryCardId = userDto.LibraryCardId,
				FirstName = userDto.FirstName ?? string.Empty,
				LastName = userDto.LastName ?? string.Empty, 
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
