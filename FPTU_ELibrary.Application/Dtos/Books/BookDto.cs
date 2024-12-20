using FPTU_ELibrary.Application.Dtos.Employees;
using FPTU_ELibrary.Domain.Entities;

namespace FPTU_ELibrary.Application.Dtos.Books
{
    public class BookDto
    {
        public int BookId { get; set; }

        public string Title { get; set; } = null!;

        public string? Summary { get; set; }

        public int CategoryId { get; set; }

        public bool IsDeleted { get; set; }

        public bool IsDraft { get; set; }

        public bool CanBorrow { get; set; }

        public DateTime CreateDate { get; set; }

        public Guid CreateBy { get; set; }

        public DateTime? UpdatedDate { get; set; }

        public Guid? UpdatedBy { get; set; }

        public ICollection<BookEditionAuthorDto> BookAuthors { get; set; } = new List<BookEditionAuthorDto>();
        public ICollection<BookEditionDto> BookEditions { get; set; } = new List<BookEditionDto>();
        public ICollection<BookCategoryDto> BookCategories { get; set; } = new List<BookCategoryDto>();
        public EmployeeDto CreateByNavigation { get; set; } = null!;
        public EmployeeDto? UpdatedByNavigation { get; set; }
    }
}
