using System.Linq.Expressions;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Specifications.Params;
using Microsoft.EntityFrameworkCore;

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
                    !string.IsNullOrEmpty(a.Author.AuthorCode) && a.Author.AuthorCode.Contains(specParams.Search) || 
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
        
        // Apply include
        ApplyInclude(q => q
            .Include(e => e.Book)
                .ThenInclude(e => e.BookCategories)
            .Include(e => e.BookEditionAuthors)
                .ThenInclude(e => e.Author)
            .Include(b => b.BookEditionCopies)
        );
        
        // Default order by 
        AddOrderBy(e => e.Book.Title);
        
        // Book edition properties
        if (specParams.EditionNumberRange != null 
            && specParams.EditionNumberRange.Any()) // Range edition number
        {
            AddFilter(x => specParams.EditionNumberRange.Contains(x.EditionNumber));
        }
        if (specParams.PublicationYearRange != null 
            && specParams.PublicationYearRange.Any()) // Range publication years
        {
            AddFilter(x => specParams.PublicationYearRange.Contains(x.PublicationYear));
        }
        if (specParams.PageCountRange != null 
            && specParams.PageCountRange.Any()) // Range page count
        {
            AddFilter(x => specParams.PageCountRange.Contains(x.PageCount));
        }
        if (!string.IsNullOrEmpty(specParams.Format)) // With specific format
        {
            AddFilter(x => x.Format == specParams.Format);
        }
        if (!string.IsNullOrEmpty(specParams.Language)) // With specific lang
        {
            AddFilter(x => x.Language == specParams.Language);
        }
        if (specParams.CanBorrow != null) // Can borrow status
        {
            AddFilter(x => x.CanBorrow == specParams.CanBorrow);
        }
        if (specParams.IsDeleted != null) // Is deleted
        {
            AddFilter(x => x.IsDeleted == specParams.IsDeleted);       
        }
        
        // Book edition author properties
        if (specParams.EditionNumberRange != null 
            && specParams.EditionNumberRange.Any()) // Range author dob
        {
            AddFilter(x => specParams.EditionNumberRange.Contains(x.EditionNumber));
        }
        if (!string.IsNullOrEmpty(specParams.AuthorCode)) // With specific author code
        {
            AddFilter(x => 
                x.BookEditionAuthors.Select(bea => bea.Author)
                    .Any(a => a.AuthorCode == specParams.AuthorCode));
        }
        if (!string.IsNullOrEmpty(specParams.AuthorCode)) // With specific author name
        {
            AddFilter(x => 
                x.BookEditionAuthors.Select(bea => bea.Author)
                    .Any(a => a.FullName == specParams.AuthorFullName));
        }
        if (!string.IsNullOrEmpty(specParams.AuthorNationality)) // With specific author nation
        {
            AddFilter(x => 
                x.BookEditionAuthors.Select(bea => bea.Author)
                    .Any(a => a.Nationality == specParams.AuthorNationality));
        }
        if (specParams.AuthorDobRange != null 
            && specParams.AuthorDobRange.Count > 1) // With range of author dob
        {
            AddFilter(x => 
                x.BookEditionAuthors.Select(bea => bea.Author)
                    .Any(a => a.Dob >= specParams.AuthorDobRange[0].Date && 
                              a.Dob <= specParams.AuthorDobRange[1].Date));
        }
        if (specParams.AuthorDateOfDeathRange != null 
            && specParams.AuthorDateOfDeathRange.Count > 1) // With range of author date of death
        {
            AddFilter(x => 
                x.BookEditionAuthors.Select(bea => bea.Author)
                    .Any(a => a.DateOfDeath >= specParams.AuthorDateOfDeathRange[0].Date && 
                              a.DateOfDeath <= specParams.AuthorDateOfDeathRange[1].Date));
        }
        
        // Book edition copy properties
        if (specParams.ShelfId != null) // With specific shelf
        {
            AddFilter(x => x.ShelfId == specParams.ShelfId);       
        }
        if (!string.IsNullOrEmpty(specParams.BookEditionCopyCode)) // With copy code
        {
            AddFilter(x => x.BookEditionCopies.Any(bec => 
                bec.Code == specParams.BookEditionCopyCode));       
        }
        if (!string.IsNullOrEmpty(specParams.BookEditionCopyStatus)) // With copy status
        {
            AddFilter(x => x.BookEditionCopies.Any(bec => 
                bec.Status == specParams.BookEditionCopyStatus));       
        }
        
        // Book properties
        // TODO: Update filter date range
        if (specParams.IsDraft != null) // With status
        {
            AddFilter(x => x.Book.IsDraft == specParams.IsDraft);       
        }
        if (specParams.CreatedAtRange != null 
            && specParams.CreatedAtRange.Count > 1) // With range of create date 
        {
            AddFilter(x => 
                x.CreatedAt >= specParams.CreatedAtRange[0].Date 
                && x.CreatedAt <= specParams.CreatedAtRange[1].Date);       
        }
        if (specParams.UpdatedAtRange != null 
            && specParams.UpdatedAtRange.Count > 1) // With range of create date 
        {
            AddFilter(x => x.UpdatedAt.HasValue &&
                           x.UpdatedAt.Value.Date >= specParams.UpdatedAtRange[0].Date 
                           && x.UpdatedAt.Value.Date <= specParams.UpdatedAtRange[1].Date);       
        }
        
        // Progress sorting
        if (!string.IsNullOrEmpty(specParams.Sort))
        {
            // Check is descending sorting 
            var isDescending = specParams.Sort.StartsWith("-");
            if (isDescending)
            {
                specParams.Sort = specParams.Sort.Trim('-');
            }

            specParams.Sort = specParams.Sort.ToUpper();
            
            // Define sorting pattern
            var sortMappings = new Dictionary<string, Expression<Func<BookEdition, object>>>()
            {
                { "EDITIONNUMBER", x => x.EditionNumber },
                { "PUBLICATIONYEAR", x => x.PublicationYear },
                { "PAGECOUNT", x => x.PageCount },
                { "TOTALCOPIES", x => x.BookEditionInventory!.TotalCopies },
                { "AVAILABLECOPIES", x => x.BookEditionInventory!.AvailableCopies },
                { "REQUESTCOPIES", x => x.BookEditionInventory!.RequestCopies },
                { "RESERVEDCOPIES", x => x.BookEditionInventory!.RequestCopies },
                { "AUTHOR", x => x.BookEditionAuthors.Select(bea => bea.Author.FullName) },
                { "TITLE", x => x.Book.Title },
                { "EDITIONTITLE", x => x.EditionTitle! },
                { "SHELF", x => x.Shelf != null ? x.Shelf.ShelfNumber : null! },
                { "FORMAT", x => x.Format ?? null! },
                { "ISBN", x => x.Isbn },
                { "LANGUAGE", x => x.Language },
                { "COVERIMAGE", x => x.CoverImage ?? null! },
                { "PUBLISHER", x => x.Publisher ?? null! },
                { "CREATEBY", x => x.CreatedBy },
                { "CREATEDAT", x => x.CreatedAt },
                { "UPDATEDAT", x => x.UpdatedAt ?? null! },
                { "CATEGORIES", x => x.Book.BookCategories.Select(bc => bc.Category.EnglishName) },
            };
        
            // Get sorting pattern
            if (sortMappings.TryGetValue(specParams.Sort.ToUpper(), 
                    out var sortExpression))
            {
                if (isDescending) AddOrderByDescending(sortExpression);
                else AddOrderBy(sortExpression);
                if (specParams.Sort == "AUTHOR")
                {
                    var authorExpression = (Expression<Func<BookEdition, object>>)(x =>
                        x.BookEditionAuthors
                            .OrderBy(bea => bea.Author.FullName)
                            .Select(bea => bea.Author.FullName).FirstOrDefault() ?? string.Empty);

                    if (isDescending)
                    {
                        AddOrderByDescending(authorExpression);
                    }
                    else
                    {
                        AddOrderBy(authorExpression);
                    }
                }
                else if (specParams.Sort == "CATEGORIES")
                {
                    var categoriesExpression = (Expression<Func<BookEdition, object>>)(x =>
                        x.Book.BookCategories
                            .OrderBy(bc => bc.Category.EnglishName)
                            .Select(bc => bc.Category.EnglishName).FirstOrDefault() ?? string.Empty);

                    if (isDescending)
                    {
                        AddOrderByDescending(categoriesExpression);
                    }
                    else
                    {
                        AddOrderBy(categoriesExpression);
                    }
                }
                else
                {
                    // Default sorting logic for other fields
                    if (isDescending) AddOrderByDescending(sortExpression);
                    else AddOrderBy(sortExpression);
                }
            }
        }
    }
}