using FPTU_ELibrary.Domain.Interfaces;

namespace FPTU_ELibrary.Domain.Entities;

public class LibraryItemGroup : IAuditableEntity
{
    public int GroupId { get; set; }
    public string AiTrainingCode { get; set; } = null!;
    public string ClassificationNumber { get; set; } = null!;
    public string CutterNumber { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string? SubTitle { get; set; }
    public string Author { get; set; } = null!;
    public string? TopicalTerms { get; set; }
    
    // Creation, update datetime and employee is charge of 
    public DateTime? TrainedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string CreatedBy { get; set; } = null!;
    public string? UpdatedBy { get; set; }
    
    // Mapping entities
    public ICollection<LibraryItem> LibraryItems { get; set; } = new List<LibraryItem>();
}