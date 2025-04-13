using System.Linq.Expressions;
using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Specifications.Interfaces;
using FPTU_ELibrary.Domain.Specifications.Params;
using Microsoft.EntityFrameworkCore;

namespace FPTU_ELibrary.Domain.Specifications;

public class DigitalBorrowSpecification : BaseSpecification<DigitalBorrow>
{
    public int PageIndex { get; set; }
    public int PageSize { get; set; }
    public string? Email { get; set; }
    public Guid? UserId { get; set; }

    public DigitalBorrowSpecification(DigitalBorrowSpecParams specParams, int pageIndex, int pageSize,
        string? email = null, Guid? userId = null)
        : base(d =>
            // Search with terms
            string.IsNullOrEmpty(specParams.Search) ||
            (
                // Status
                d.Status.ToString().Contains(specParams.Search) || 
                // Library resource
                (!string.IsNullOrEmpty(d.LibraryResource.ResourceTitle) && d.LibraryResource.ResourceTitle.Contains(specParams.Search)) || 
                (!string.IsNullOrEmpty(d.LibraryResource.ResourceType) && d.LibraryResource.ResourceType.Contains(specParams.Search)) || 
                (!string.IsNullOrEmpty(d.LibraryResource.Provider) && d.LibraryResource.Provider.Contains(specParams.Search)) || 
                (!string.IsNullOrEmpty(d.LibraryResource.FileFormat) && d.LibraryResource.FileFormat.Contains(specParams.Search)) || 
                (!string.IsNullOrEmpty(d.LibraryResource.ProviderMetadata) && d.LibraryResource.ProviderMetadata.Contains(specParams.Search))
            )
        )
    {
        // Pagination
        PageIndex = pageIndex;
        PageSize = pageSize;
        // Assign default fields (if any)
        UserId = userId;
        Email = email;
        
        // Enable split query
        EnableSplitQuery();
        
        // Apply include
        ApplyInclude(q => q
            .Include(d => d.User)
            .Include(d => d.LibraryResource)
        );
        
        // Add filter 
        if (!string.IsNullOrEmpty(email)) // by Email
        {
            AddFilter(d => d.User.Email == email);
        }
        if (userId.HasValue && userId != Guid.Empty) // by UserId
        {
            AddFilter(d => d.UserId == userId);
        }
        if (specParams.IsExtended != null) // Is extend
        {
            AddFilter(d => d.IsExtended == specParams.IsExtended);
        }
        if (specParams.Status != null) // Status
        {
            AddFilter(d => d.Status == specParams.Status);
        }
        if (specParams.RegisterDateRange != null
            && specParams.RegisterDateRange.Length > 1) // With range of register date
        {
            if (specParams.RegisterDateRange[0].HasValue && specParams.RegisterDateRange[1].HasValue)
            {
                AddFilter(x =>
                    x.RegisterDate.Date >= specParams.RegisterDateRange[0]!.Value.Date
                    && x.RegisterDate.Date <= specParams.RegisterDateRange[1]!.Value.Date);
            }
            else if (specParams.RegisterDateRange[0] is null && specParams.RegisterDateRange[1].HasValue)
            {
                AddFilter(x => x.RegisterDate.Date <= specParams.RegisterDateRange[1]);
            }
            else if (specParams.RegisterDateRange[0].HasValue && specParams.RegisterDateRange[1] is null)
            {
                AddFilter(x => x.RegisterDate.Date >= specParams.RegisterDateRange[0]);
            }
        }
        if (specParams.ExpiryDateRange != null
            && specParams.ExpiryDateRange.Length > 1) // With range of expiry date
        {
            if (specParams.ExpiryDateRange[0].HasValue && specParams.ExpiryDateRange[1].HasValue)
            {
                AddFilter(x =>
                    x.ExpiryDate.Date >= specParams.ExpiryDateRange[0]!.Value.Date
                    && x.ExpiryDate.Date <= specParams.ExpiryDateRange[1]!.Value.Date);
            }
            else if (specParams.ExpiryDateRange[0] is null && specParams.ExpiryDateRange[1].HasValue)
            {
                AddFilter(x => x.ExpiryDate.Date <= specParams.ExpiryDateRange[1]);
            }
            else if (specParams.ExpiryDateRange[0].HasValue && specParams.ExpiryDateRange[1] is null)
            {
                AddFilter(x => x.ExpiryDate.Date >= specParams.ExpiryDateRange[0]);
            }
        }
        
        // Advanced filter
        if (specParams.F != null && specParams.F.Any())
        {
            // Convert to advanced filter list
            var filerList = specParams.FromParamsToListAdvancedFilter();
            if (filerList != null)
            {
                foreach (var filter in filerList)
                {
                    // Resource type
                    if (filter.FieldName.ToLowerInvariant() == nameof(LibraryResource.ResourceType).ToLowerInvariant())
                    {
                        if(Enum.TryParse(typeof(LibraryResourceType), filter.Value, out var validResourceType))
                        {
                            // Determine operator
                            switch (filter.Operator)
                            {
                                case FilterOperator.Equals:
                                    AddFilter(d => d.LibraryResource.ResourceType == validResourceType.ToString());
                                    break;
                                case FilterOperator.NotEqualsTo:
                                    AddFilter(d => d.LibraryResource.ResourceType != validResourceType.ToString());
                                    break;
                            }
                        }
                    }
                    // Resource size
                    else if (filter.FieldName.ToLowerInvariant() == nameof(LibraryResource.ResourceSize).ToLowerInvariant())
                    {
                        if (decimal.TryParse(filter.Value, out var validSizeNum))
                        {
                            // Determine operator
                            switch (filter.Operator)
                            {
                                case FilterOperator.Equals:
                                    AddFilter(d => d.LibraryResource.ResourceSize == validSizeNum);
                                    break;
                                case FilterOperator.NotEqualsTo:
                                    AddFilter(d => d.LibraryResource.ResourceSize != validSizeNum);
                                    break;
                                case FilterOperator.GreaterThan:
                                    AddFilter(d => d.LibraryResource.ResourceSize > validSizeNum);
                                    break;
                                case FilterOperator.GreaterThanOrEqualsTo:
                                    AddFilter(d => d.LibraryResource.ResourceSize >= validSizeNum);
                                    break;
                                case FilterOperator.LessThan:
                                    AddFilter(d => d.LibraryResource.ResourceSize < validSizeNum);
                                    break;
                                case FilterOperator.LessThanOrEqualsTo:
                                    AddFilter(d => d.LibraryResource.ResourceSize <= validSizeNum);
                                    break;
                            }
                        }
                    }
                    // Borrow price
                    else if (filter.FieldName.ToLowerInvariant() == nameof(LibraryResource.BorrowPrice).ToLowerInvariant())
                    {
                        if (decimal.TryParse(filter.Value, out var validSizeNum))
                        {
                            // Determine operator
                            switch (filter.Operator)
                            {
                                case FilterOperator.Equals:
                                    AddFilter(d => d.LibraryResource.BorrowPrice == validSizeNum);
                                    break;
                                case FilterOperator.NotEqualsTo:
                                    AddFilter(d => d.LibraryResource.BorrowPrice != validSizeNum);
                                    break;
                                case FilterOperator.GreaterThan:
                                    AddFilter(d => d.LibraryResource.BorrowPrice > validSizeNum);
                                    break;
                                case FilterOperator.GreaterThanOrEqualsTo:
                                    AddFilter(d => d.LibraryResource.BorrowPrice >= validSizeNum);
                                    break;
                                case FilterOperator.LessThan:
                                    AddFilter(d => d.LibraryResource.BorrowPrice < validSizeNum);
                                    break;
                                case FilterOperator.LessThanOrEqualsTo:
                                    AddFilter(d => d.LibraryResource.BorrowPrice <= validSizeNum);
                                    break;
                            }
                        }
                    }
                    // DefaultBorrowDurationDays
                    else if (filter.FieldName.ToLowerInvariant() == nameof(LibraryResource.DefaultBorrowDurationDays).ToLowerInvariant())
                    {
                        if (int.TryParse(filter.Value, out var validDurationDays))
                        {
                            // Determine operator
                            switch (filter.Operator)
                            {
                                case FilterOperator.Equals:
                                    AddFilter(d => d.LibraryResource.DefaultBorrowDurationDays == validDurationDays);
                                    break;
                                case FilterOperator.NotEqualsTo:
                                    AddFilter(d => d.LibraryResource.DefaultBorrowDurationDays != validDurationDays);
                                    break;
                                case FilterOperator.GreaterThan:
                                    AddFilter(d => d.LibraryResource.DefaultBorrowDurationDays > validDurationDays);
                                    break;
                                case FilterOperator.GreaterThanOrEqualsTo:
                                    AddFilter(d => d.LibraryResource.DefaultBorrowDurationDays >= validDurationDays);
                                    break;
                                case FilterOperator.LessThan:
                                    AddFilter(d => d.LibraryResource.DefaultBorrowDurationDays < validDurationDays);
                                    break;
                                case FilterOperator.LessThanOrEqualsTo:
                                    AddFilter(d => d.LibraryResource.DefaultBorrowDurationDays <= validDurationDays);
                                    break;
                            }
                        }
                    }
                }
            }
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
            AddOrderByDescending(n => n.RegisterDate);
        }
    }
    
    private void ApplySorting(string propertyName, bool isDescending)
    {
        if (string.IsNullOrEmpty(propertyName)) return;

        // Initialize expression parameter with type of LibraryItem (x)
        var parameter = Expression.Parameter(typeof(DigitalBorrow), "x");
        // Assign property base on property name (x.PropertyName)
        var property = Expression.Property(parameter, propertyName);
        // Building a complete sort lambda expression (x => x.PropertyName)
        var sortExpression =
            Expression.Lambda<Func<DigitalBorrow, object>>(Expression.Convert(property, typeof(object)), parameter);

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

public static class DigitalBorrowSpecificationExtensions
{
    public static List<AdvancedFilter>? FromParamsToListAdvancedFilter(this DigitalBorrowSpecParams specParams)
    {
        if (specParams.F == null || !specParams.F.Any()) return null;
        
        // Initialize list of advanced filters
        var advancedFilters = new List<AdvancedFilter>();
        for (int i = 0; i < specParams.F.Length; i++)
        {
            var fieldName = specParams.F[i];
            var filterOperator = specParams.O?.ElementAtOrDefault(i) != null ? specParams.O[i] : (FilterOperator?) null;
            var value = specParams.V?.ElementAtOrDefault(i) != null ? specParams.V[i] : null;

            if (filterOperator != null && value != null)
            {
                advancedFilters.Add(new()
                {
                    FieldName = fieldName,
                    Operator = filterOperator,
                    Value = value,
                });
            }
        }

        return advancedFilters;
    }
}