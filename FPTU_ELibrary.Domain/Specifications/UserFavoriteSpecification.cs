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
                uf.LibraryItem.SubTitle.Contains(specParams.Search) ||
                uf.LibraryItem.Responsibility.Contains(specParams.Search) ||
                uf.LibraryItem.Edition.Contains(specParams.Search) ||
                uf.LibraryItem.Language.Contains(specParams.Search) ||
                uf.LibraryItem.OriginLanguage.Contains(specParams.Search) ||
                uf.LibraryItem.Summary.Contains(specParams.Search) ||
                uf.LibraryItem.Publisher.Contains(specParams.Search) ||
                uf.LibraryItem.PublicationPlace.Contains(specParams.Search) ||
                uf.LibraryItem.ClassificationNumber.Contains(specParams.Search) ||
                uf.LibraryItem.CutterNumber.Contains(specParams.Search) ||
                uf.LibraryItem.Isbn.Contains(specParams.Search) ||
                uf.LibraryItem.Ean.Contains(specParams.Search) ||
                uf.LibraryItem.PhysicalDetails.Contains(specParams.Search) ||
                uf.LibraryItem.Dimensions.Contains(specParams.Search) ||
                uf.LibraryItem.AccompanyingMaterial.Contains(specParams.Search) ||
                uf.LibraryItem.Genres.Contains(specParams.Search) ||
                uf.LibraryItem.GeneralNote.Contains(specParams.Search) ||
                uf.LibraryItem.BibliographicalNote.Contains(specParams.Search) ||
                uf.LibraryItem.TopicalTerms.Contains(specParams.Search) ||
                uf.LibraryItem.AdditionalAuthors.Contains(specParams.Search)
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

        AddFilter(uf => uf.User.Email.Equals(email));
        // Apply filters
        if (!string.IsNullOrEmpty(specParams.Title))
        {
            AddFilter(uf => uf.LibraryItem.Title.Contains(specParams.Title));
        }

        if (!string.IsNullOrEmpty(specParams.Author))
        {
            AddFilter(uf => uf.LibraryItem.LibraryItemAuthors.Any(a => a.Author.FullName.Contains(specParams.Author)));
        }

        if (!string.IsNullOrEmpty(specParams.Isbn))
        {
            AddFilter(uf => uf.LibraryItem.Isbn.Contains(specParams.Isbn));
        }

        if (!string.IsNullOrEmpty(specParams.ClassificationNumber))
        {
            AddFilter(uf => uf.LibraryItem.ClassificationNumber.Contains(specParams.ClassificationNumber));
        }

        if (!string.IsNullOrEmpty(specParams.Genres))
        {
            AddFilter(uf => uf.LibraryItem.Genres.Contains(specParams.Genres));
        }

        if (!string.IsNullOrEmpty(specParams.Publisher))
        {
            AddFilter(uf => uf.LibraryItem.Publisher.Contains(specParams.Publisher));
        }

        if (!string.IsNullOrEmpty(specParams.TopicalTerms))
        {
            AddFilter(uf => uf.LibraryItem.TopicalTerms.Contains(specParams.TopicalTerms));
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