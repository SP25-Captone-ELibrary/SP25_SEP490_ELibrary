using System.Text.Json.Serialization;
using FPTU_ELibrary.Domain.Entities.Base;

namespace FPTU_ELibrary.Domain.Entities;

public class Employee : BaseUser
{
    // Key
    public Guid EmployeeId { get; set; }

    // Employee detail information
    public string? EmployeeCode { get; set; }
    
    // Employee join, terminate date
    public DateTime? HireDate { get; set; }
    public DateTime? TerminationDate { get; set; }
    
    // Role in the system
    public int RoleId { get; set; }

    // Mapping entities
    public SystemRole Role { get; set; } = null!;

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
