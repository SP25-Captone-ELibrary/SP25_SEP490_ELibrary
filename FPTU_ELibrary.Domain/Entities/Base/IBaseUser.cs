namespace FPTU_ELibrary.Domain.Entities.Base;

public interface IBaseUser
{
    // Basic user information
    string Email { get; set; } 
    string FirstName { get; set; }
    string LastName { get; set; }
    string? PasswordHash { get; set; }
    string? Phone { get; set; }
    string? Avatar { get; set; }
    string? Address { get; set; }
    string? Gender { get; set; }
    DateTime? Dob { get; set; }
    
    // Mark as active or not
    bool IsActive { get; set; }
    
    // Creation and modify date
    DateTime CreateDate { get; set; }
    DateTime? ModifiedDate { get; set; }
    
    // Multi-factor authentication properties
    bool TwoFactorEnabled { get; set; }
    bool PhoneNumberConfirmed { get; set; }
    bool EmailConfirmed { get; set; }
    string? TwoFactorSecretKey { get; set; }
    string? TwoFactorBackupCodes { get; set; }
    string? PhoneVerificationCode { get; set; }
    string? EmailVerificationCode { get; set; }
    DateTime? PhoneVerificationExpiry { get; set; }
}