using FPTU_ELibrary.Application.Dtos.BookEditions;

namespace FPTU_ELibrary.Application.Dtos.Books
{
    public class BookDto
    {
        // Key
        public int BookId { get; set; }

        // Book information
        public string Title { get; set; } = null!;
        public string? SubTitle { get; set; }
        public string? Summary { get; set; }
        
        // Unique string to manage books (group editions / AI training)
        public string BookCode { get; set; } = null!;
        public Guid? BookCodeForAITraining { get; set; }
    
        // Book management and borrow permission
        public bool IsDeleted { get; set; }
        
        // Datetime and employee who create or update the book
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string CreatedBy { get; set; } = null!;
        public string? UpdatedBy { get; set; } 

        // Mapping entities
        public ICollection<BookEditionDto> BookEditions { get; set; } = new List<BookEditionDto>();
        public ICollection<BookCategoryDto> BookCategories { get; set; } = new List<BookCategoryDto>();
        public ICollection<BookResourceDto> BookResources { get; set; } = new List<BookResourceDto>();
    }
}
