using System.Drawing;
using System.Linq.Expressions;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Specifications.Params;
using Microsoft.EntityFrameworkCore;

namespace FPTU_ELibrary.Domain.Specifications;

public class LibraryShelfSpecification : BaseSpecification<LibraryShelf>
{
    public int PageIndex { get; set; }
    public int PageSize { get; set; }
    
    public LibraryShelfSpecification(LibraryShelfSpecParams specParams, int pageIndex, int pageSize)
        : base(s => 
            string.IsNullOrEmpty(specParams.Search) || 
            (
                !string.IsNullOrEmpty(s.ShelfNumber) && s.ShelfNumber.Contains(specParams.Search) ||
                !string.IsNullOrEmpty(s.EngShelfName) && s.EngShelfName.Contains(specParams.Search) || 
                !string.IsNullOrEmpty(s.VieShelfName) && s.VieShelfName.Contains(specParams.Search) ||
                s.ClassificationNumberRangeFrom == specParams.ParsedSearchDecimal ||
                s.ClassificationNumberRangeTo == specParams.ParsedSearchDecimal ||
                // LibrarySection
                !string.IsNullOrEmpty(s.Section.EngSectionName) && s.Section.EngSectionName.Contains(specParams.Search) ||
                !string.IsNullOrEmpty(s.Section.VieSectionName) && s.Section.VieSectionName.Contains(specParams.Search) ||
                !string.IsNullOrEmpty(s.Section.ShelfPrefix) && s.Section.ShelfPrefix.Contains(specParams.Search) ||
                s.Section.ClassificationNumberRangeFrom == specParams.ParsedSearchDecimal ||
                s.Section.ClassificationNumberRangeTo == specParams.ParsedSearchDecimal ||
                // LibraryZone
                !string.IsNullOrEmpty(s.Section.Zone.EngZoneName) && s.Section.Zone.EngZoneName.Contains(specParams.Search) ||
                !string.IsNullOrEmpty(s.Section.Zone.VieZoneName) && s.Section.Zone.VieZoneName.Contains(specParams.Search) ||
                !string.IsNullOrEmpty(s.Section.Zone.EngDescription) && s.Section.Zone.EngDescription.Contains(specParams.Search) ||
                !string.IsNullOrEmpty(s.Section.Zone.VieDescription) && s.Section.Zone.VieDescription.Contains(specParams.Search)
            )
        )
    {
        // Pagination
        PageIndex = pageIndex;
        PageSize = pageSize;
        
        // Apply include
        ApplyInclude(q => q
            .Include(s => s.Section)
                .ThenInclude(sec => sec.Zone)
                    .ThenInclude(z => z.Floor)
        );
        
        // Add filter
        if (specParams.IsChildrenSection != null) // Is children section
        {
            AddFilter(s => s.Section.IsChildrenSection == specParams.IsChildrenSection);
        }
        if (specParams.IsJournalSection != null) // Is journal section
        {
            AddFilter(s => s.Section.IsJournalSection == specParams.IsJournalSection);
        }
        if (specParams.IsReferenceSection != null)
        {
            AddFilter(s => s.Section.IsReferenceSection == specParams.IsReferenceSection);            
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
            
            // Uppercase sort value
            specParams.Sort = specParams.Sort.ToUpper();

            // Apply sorting
            ApplySorting(specParams.Sort, isDescending);
        }
        else
        {
            // Default order by shelf number
            AddOrderBy(u => u.ShelfNumber);
        }
    }
    
    private void ApplySorting(string propertyName, bool isDescending)
    {
        if (string.IsNullOrEmpty(propertyName)) return;

        // Initialize expression parameter with type of LibraryShelf (x)
        var parameter = Expression.Parameter(typeof(LibraryShelf), "x");
        // Assign property base on property name (x.PropertyName)
        var property = Expression.Property(parameter, propertyName);
        // Building a complete sort lambda expression (x => x.PropertyName)
        var sortExpression =
            Expression.Lambda<Func<LibraryShelf, object>>(Expression.Convert(property, typeof(object)), parameter);

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