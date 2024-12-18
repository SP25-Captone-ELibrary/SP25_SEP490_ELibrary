namespace FPTU_ELibrary.Domain.Entities;

public class Book
{
    // Key
    public int BookId { get; set; }

    // Book information
    public string Title { get; set; } = null!;
    public string? Summary { get; set; }
    
    // Book management and borrow permission
    public bool IsDeleted { get; set; }
    public bool IsDraft { get; set; }
    public bool CanBorrow { get; set; }

    // Book category
    public int CategoryId { get; set; }
    
    // Datetime and employee who create or update the book
    public DateTime CreateDate { get; set; }
    public Guid CreateBy { get; set; }
    public DateTime? UpdatedDate { get; set; }
    public Guid? UpdatedBy { get; set; }

    // Mapping entities
    public ICollection<BookEdition> BookEditions { get; set; } = new List<BookEdition>();
    public BookCategory Category { get; set; } = null!;
    public Employee CreateByNavigation { get; set; } = null!;
    public Employee? UpdatedByNavigation { get; set; }
}
