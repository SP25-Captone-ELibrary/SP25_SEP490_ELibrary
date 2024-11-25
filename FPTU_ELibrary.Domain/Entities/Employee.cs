using System.Text.Json.Serialization;

namespace FPTU_ELibrary.Domain.Entities;

public class Employee
{
    // Key
    public Guid EmployeeId { get; set; }

    // Employee detail information
    public string? EmployeeCode { get; set; }
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
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
    public int JobRoleId { get; set; }

    // Mapping entities
    public JobRole JobRole { get; set; } = null!;

    [JsonIgnore]
    public ICollection<Book> BookCreateByNavigations { get; set; } = new List<Book>();

	[JsonIgnore]
	public ICollection<Book> BookUpdatedByNavigations { get; set; } = new List<Book>();

	[JsonIgnore]
    public ICollection<BookEdition> BookEditions { get; set; } = new List<BookEdition>();
    
    [JsonIgnore]
    public ICollection<BookResource> BookResources { get; set; } = new List<BookResource>();

    [JsonIgnore]
    public ICollection<BorrowRecord> BorrowRecords { get; set; } = new List<BorrowRecord>();
    
    [JsonIgnore]
    public ICollection<CopyConditionHistory> CopyConditionHistories { get; set; } = new List<CopyConditionHistory>();
    
    [JsonIgnore]
    public ICollection<Fine> FineCreateByNavigations { get; set; } = new List<Fine>();

    [JsonIgnore]
    public ICollection<LearningMaterial> LearningMaterialCreateByNavigations { get; set; } = new List<LearningMaterial>();

    //public ICollection<LearningMaterial> LearningMaterialUpdatedByNavigations { get; set; } = new List<LearningMaterial>();
    //public ICollection<BorrowRequest> BorrowRequests { get; set; } = new List<BorrowRequest>();

    [JsonIgnore]
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();

	[JsonIgnore]
	public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
