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
    public ICollection<BorrowRecord> BorrowRecords { get; set; } = new List<BorrowRecord>();
    
    [JsonIgnore]
    public ICollection<BorrowRecordDetail> BorrowRecordDetails { get; set; } = new List<BorrowRecordDetail>();
    
    [JsonIgnore]
    public ICollection<Fine> FineCreateByNavigations { get; set; } = new List<Fine>();

    [JsonIgnore]
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();

	[JsonIgnore]
	public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
