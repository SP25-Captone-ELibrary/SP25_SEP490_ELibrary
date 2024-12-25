using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Specifications.Params;

namespace FPTU_ELibrary.Domain.Specifications;

public class BookEditionSpecification : BaseSpecification<BookEdition>
{
    public int PageIndex { get; set; }
    public int PageSize { get; set; }
    
    public BookEditionSpecification(BookEditionSpecParams specParams, int pageIndex, int pageSize)
        : base(be => 
            string.IsNullOrEmpty(specParams.Search) || (
                // Book Editions
                (!string.IsNullOrEmpty(be.EditionTitle) && be.EditionTitle.Contains(specParams.Search)) ||
                (!string.IsNullOrEmpty(be.EditionSummary) && be.EditionSummary.Contains(specParams.Search)) ||
                (!string.IsNullOrEmpty(be.Format) && be.Format.Contains(specParams.Search)) ||
                (!string.IsNullOrEmpty(be.Isbn) && be.Isbn.Contains(specParams.Search)) ||
                (!string.IsNullOrEmpty(be.Publisher) && be.Publisher.Contains(specParams.Search)) || 
                // Book
                (!string.IsNullOrEmpty(be.Book.Title) && be.Book.Title.Contains(specParams.Search)) ||
                (!string.IsNullOrEmpty(be.Book.SubTitle) && be.Book.SubTitle.Contains(specParams.Search)) ||
                (!string.IsNullOrEmpty(be.Book.Summary) && be.Book.Summary.Contains(specParams.Search)) ||
                // BookEditionAuthors
                // BookEditions -> BookEditionAuthors
                be.BookEditionAuthors.Any(a =>
                    !string.IsNullOrEmpty(a.Author.FullName) && a.Author.FullName.Contains(specParams.Search) || 
                    !string.IsNullOrEmpty(a.Author.Biography) && a.Author.Biography.Contains(specParams.Search) || 
                    !string.IsNullOrEmpty(a.Author.Nationality) && a.Author.Nationality.Contains(specParams.Search) 
                )
            )
        )
    {
        // Assign page size and page index
        PageIndex = pageIndex;
        PageSize = pageSize;
        
        // Enable split query
        EnableSplitQuery();
        
        // Default order by 
        AddOrderBy(e => e.Book.Title);
    }
}