using System.Text.Json.Serialization;

namespace FPTU_ELibrary.Domain.Entities;

public class LibraryCardPackage
{
    // Key
    public int LibraryCardPackageId { get; set; }
    
    // Name 
    public string PackageName { get; set; } = null!;
    
    // Price of the package 
    public decimal Price { get; set; }
    
    // Duration of the package in months 
    public int DurationInMonths { get; set; }
    
    // Active 
    public bool IsActive { get; set; }
    
    // Creation date
    public DateTime CreatedAt { get; set; }
    
    // Description
    public string Description { get; set; } = null!;
    
    // Transaction
    [JsonIgnore]
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}