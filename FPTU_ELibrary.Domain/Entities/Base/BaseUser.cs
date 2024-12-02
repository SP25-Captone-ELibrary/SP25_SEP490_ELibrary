namespace FPTU_ELibrary.Domain.Entities.Base;

public class BaseUser : IBaseUser
{
    // Basic user information
    public string Email { get; set; } = null!;
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string? PasswordHash { get; set; }
    public string? Phone { get; set; }
    public string? Avatar { get; set; }
    public string? Address { get; set; }
    public string? Gender { get; set; }
    public DateTime? Dob { get; set; }

    // Mark as active or not
    public bool IsActive { get; set; }
    
    // Creation and modify date
    public DateTime CreateDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
    
    // Multi-factor authentication properties
    public bool TwoFactorEnabled { get; set; }
    public bool PhoneNumberConfirmed { get; set; }
    public bool EmailConfirmed { get; set; }
    public string? TwoFactorSecretKey { get; set; }
    public string? TwoFactorBackupCodes { get; set; }
    public string? PhoneVerificationCode { get; set; }
    public string? EmailVerificationCode { get; set; }
    public DateTime? PhoneVerificationExpiry { get; set; }
}