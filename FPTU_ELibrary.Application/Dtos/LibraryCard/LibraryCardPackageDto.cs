using System.Text.Json.Serialization;
using FPTU_ELibrary.Application.Dtos.Payments;

namespace FPTU_ELibrary.Application.Dtos.LibraryCard;

public class LibraryCardPackageDto
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
    public ICollection<TransactionDto> Transactions { get; set; } = new List<TransactionDto>();
}