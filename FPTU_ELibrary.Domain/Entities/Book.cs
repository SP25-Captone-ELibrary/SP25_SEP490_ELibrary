using FPTU_ELibrary.Domain.Interfaces;

namespace FPTU_ELibrary.Domain.Entities;

public class Book : IAuditableEntity
{
    // Key
    public int BookId { get; set; }
    
    #region Update 2/1/2025 update AI field
    public Guid? BookCodeForAITraining { get; set; }
    #endregion
    // Book information
    public string Title { get; set; } = null!;
    public string? SubTitle { get; set; }
    public string? Summary { get; set; }
    
    // Book management and borrow permission
    public bool IsDeleted { get; set; }
    public bool IsDraft { get; set; }
        
    // Datetime and employee who create or update the book
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string CreatedBy { get; set; } = null!;
    public string? UpdatedBy { get; set; } 

    // Mapping entities
    public ICollection<BookEdition> BookEditions { get; set; } = new List<BookEdition>();
    public ICollection<BookCategory> BookCategories { get; set; } = new List<BookCategory>();
    public ICollection<BookResource> BookResources { get; set; } = new List<BookResource>();
}
