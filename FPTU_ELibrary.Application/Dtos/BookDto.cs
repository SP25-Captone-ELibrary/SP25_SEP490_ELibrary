using FPTU_ELibrary.Domain.Entities;

namespace FPTU_ELibrary.Application.Dtos
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

        public ICollection<BookAuthor> BookAuthors { get; set; } = new List<BookAuthor>();

        public ICollection<BookEdition> BookEditions { get; set; } = new List<BookEdition>();

        public BookCategory Category { get; set; } = null!;

        public Employee CreateByNavigation { get; set; } = null!;

        public Employee? UpdatedByNavigation { get; set; }
    }
}
