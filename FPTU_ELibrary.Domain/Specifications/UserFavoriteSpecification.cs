using System.Linq.Expressions;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Specifications.Params;
using Microsoft.EntityFrameworkCore;

namespace FPTU_ELibrary.Domain.Specifications;

public class UserFavoriteSpecification : BaseSpecification<UserFavorite>
{
    public int PageIndex { get; set; }
    public int PageSize { get; set; }

    public UserFavoriteSpecification(UserFavoriteSpecParams specParams, int pageIndex, int pageSize,string email)
        : base(uf =>
            string.IsNullOrEmpty(specParams.Search) || (
                uf.LibraryItem.Title.Contains(specParams.Search) ||
                (!string.IsNullOrEmpty(uf.LibraryItem.SubTitle) && uf.LibraryItem.SubTitle.Contains(specParams.Search)) ||
                (!string.IsNullOrEmpty(uf.LibraryItem.Responsibility) && uf.LibraryItem.Responsibility.Contains(specParams.Search)) ||
                (!string.IsNullOrEmpty(uf.LibraryItem.Edition) && uf.LibraryItem.Edition.Contains(specParams.Search)) ||
                (!string.IsNullOrEmpty(uf.LibraryItem.Language) && uf.LibraryItem.Language.Contains(specParams.Search)) ||
                (!string.IsNullOrEmpty(uf.LibraryItem.OriginLanguage) && uf.LibraryItem.OriginLanguage.Contains(specParams.Search)) ||
                (!string.IsNullOrEmpty(uf.LibraryItem.Summary) && uf.LibraryItem.Summary.Contains(specParams.Search)) ||
                (!string.IsNullOrEmpty(uf.LibraryItem.Publisher) && uf.LibraryItem.Publisher.Contains(specParams.Search)) ||
                (!string.IsNullOrEmpty(uf.LibraryItem.PublicationPlace) && uf.LibraryItem.PublicationPlace.Contains(specParams.Search)) ||
                (!string.IsNullOrEmpty(uf.LibraryItem.ClassificationNumber) && uf.LibraryItem.ClassificationNumber.Contains(specParams.Search)) ||
                (!string.IsNullOrEmpty(uf.LibraryItem.CutterNumber) && uf.LibraryItem.CutterNumber.Contains(specParams.Search)) ||
                (!string.IsNullOrEmpty(uf.LibraryItem.Isbn) && uf.LibraryItem.Isbn.Contains(specParams.Search)) ||
                (!string.IsNullOrEmpty(uf.LibraryItem.Ean) && uf.LibraryItem.Ean.Contains(specParams.Search)) ||
                (!string.IsNullOrEmpty(uf.LibraryItem.PhysicalDetails) && uf.LibraryItem.PhysicalDetails.Contains(specParams.Search)) ||
                (!string.IsNullOrEmpty(uf.LibraryItem.Dimensions) && uf.LibraryItem.Dimensions.Contains(specParams.Search)) ||
                (!string.IsNullOrEmpty(uf.LibraryItem.AccompanyingMaterial) && uf.LibraryItem.AccompanyingMaterial.Contains(specParams.Search)) ||
                (!string.IsNullOrEmpty(uf.LibraryItem.Genres) && uf.LibraryItem.Genres.Contains(specParams.Search)) ||
                (!string.IsNullOrEmpty(uf.LibraryItem.GeneralNote) && uf.LibraryItem.GeneralNote.Contains(specParams.Search)) ||
                (!string.IsNullOrEmpty(uf.LibraryItem.BibliographicalNote) && uf.LibraryItem.BibliographicalNote.Contains(specParams.Search)) ||
                (!string.IsNullOrEmpty(uf.LibraryItem.TopicalTerms) && uf.LibraryItem.TopicalTerms.Contains(specParams.Search)) ||
                (!string.IsNullOrEmpty(uf.LibraryItem.AdditionalAuthors) && uf.LibraryItem.AdditionalAuthors.Contains(specParams.Search))
            )
        )
    {
        // Assign page size and page index
        PageIndex = pageIndex;
        PageSize = pageSize;
        // Apply include
        ApplyInclude(q => q
            .Include(uf => uf.LibraryItem)
            .ThenInclude(e => e.Category)
            .Include(uf => uf.LibraryItem)
            .ThenInclude(e => e.LibraryItemAuthors)
            .ThenInclude(e => e.Author)
            .Include(uf => uf.LibraryItem)
            .ThenInclude(e => e.Category)
            .Include(uf => uf.LibraryItem)
            .ThenInclude(e => e.LibraryItemInstances)
            .Include(uf => uf.User)
        );

        // Apply filter for specific user
        AddFilter(uf => uf.User.Email.Equals(email));
        
        // Apply filters
        if (!string.IsNullOrEmpty(specParams.Title)) // Title
        {
            AddFilter(uf => uf.LibraryItem.Title.Contains(specParams.Title));
        }
        if (!string.IsNullOrEmpty(specParams.Author)) // Author
        {
            AddFilter(uf => uf.LibraryItem.LibraryItemAuthors.Any(a => a.Author.FullName.Contains(specParams.Author)));
        }
        if (!string.IsNullOrEmpty(specParams.Isbn)) // Isbn
        {
            AddFilter(uf => !string.IsNullOrEmpty(uf.LibraryItem.Isbn) && uf.LibraryItem.Isbn.Contains(specParams.Isbn));
        }
        if (!string.IsNullOrEmpty(specParams.ClassificationNumber)) // Classification number
        {
            AddFilter(uf => 
                !string.IsNullOrEmpty(uf.LibraryItem.ClassificationNumber) &&
                uf.LibraryItem.ClassificationNumber.Contains(specParams.ClassificationNumber));
        }
        if (!string.IsNullOrEmpty(specParams.Genres)) // Genres
        {
            AddFilter(uf => 
                !string.IsNullOrEmpty(uf.LibraryItem.Genres) &&
                uf.LibraryItem.Genres.Contains(specParams.Genres));
        }
        if (!string.IsNullOrEmpty(specParams.Publisher)) // Publisher
        {
            AddFilter(uf => 
                !string.IsNullOrEmpty(uf.LibraryItem.Publisher) &&
                uf.LibraryItem.Publisher.Contains(specParams.Publisher));
        }
        if (!string.IsNullOrEmpty(specParams.TopicalTerms)) // Topical terms
        {
            AddFilter(uf => 
                !string.IsNullOrEmpty(uf.LibraryItem.TopicalTerms) &&
                uf.LibraryItem.TopicalTerms.Contains(specParams.TopicalTerms));
        }
        // Entry date range
        if (specParams.CreatedAtRange != null
            && specParams.CreatedAtRange.Length > 1) 
        {
            if (specParams.CreatedAtRange[0].HasValue && specParams.CreatedAtRange[1].HasValue)
            {
                AddFilter(x => 
                    x.CreatedAt.Date >= specParams.CreatedAtRange[0]!.Value.Date
                    && x.CreatedAt.Date <= specParams.CreatedAtRange[1]!.Value.Date);
            }
            else if (specParams.CreatedAtRange[0] is null && specParams.CreatedAtRange[1].HasValue)
            {
                AddFilter(x => x.CreatedAt.Date <= specParams.CreatedAtRange[1]!.Value.Date);
            }
            else if (specParams.CreatedAtRange[0].HasValue && specParams.CreatedAtRange[1] is null)
            {
                AddFilter(x => x.CreatedAt.Date >= specParams.CreatedAtRange[0]!.Value.Date);
            }
        }
        
        // Apply sorting
        if (!string.IsNullOrEmpty(specParams.Sort))
        {
            var isDescending = specParams.Sort.StartsWith("-");
            var propertyName = isDescending ? specParams.Sort.Substring(1) : specParams.Sort;

            ApplySorting(propertyName, isDescending);
        }
        else
        {
            AddOrderByDescending(uf => uf.LibraryItem.CreatedAt);
        }
    }

    private void ApplySorting(string propertyName, bool isDescending)
    {
        if (string.IsNullOrEmpty(propertyName)) return;

        var parameter = Expression.Parameter(typeof(UserFavorite), "x");
        var property = Expression.Property(parameter, propertyName);
        var sortExpression =
            Expression.Lambda<Func<UserFavorite, object>>(Expression.Convert(property, typeof(object)), parameter);

        if (isDescending)
        {
            AddOrderByDescending(sortExpression);
        }
        else
        {
            AddOrderBy(sortExpression);
        }
    }
}