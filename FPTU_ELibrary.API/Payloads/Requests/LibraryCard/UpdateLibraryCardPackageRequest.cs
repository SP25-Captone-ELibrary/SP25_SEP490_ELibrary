namespace FPTU_ELibrary.API.Payloads.Requests.LibraryCard;

public class UpdateLibraryCardPackageRequest
{
    // Name 
    public string PackageName { get; set; } = null!;
    
    // Price of the package 
    public decimal Price { get; set; }
    
    // Duration of the package in months 
    public int DurationInMonths { get; set; }
    
    // Description
    public string Description { get; set; } = null!;
}