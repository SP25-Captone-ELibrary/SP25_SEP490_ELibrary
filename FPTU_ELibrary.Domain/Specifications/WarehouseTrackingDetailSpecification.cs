using System.Linq.Expressions;
using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Specifications.Params;
using Microsoft.EntityFrameworkCore;

namespace FPTU_ELibrary.Domain.Specifications;

public class WarehouseTrackingDetailSpecification : BaseSpecification<WarehouseTrackingDetail>
{
    public int PageIndex { get; set; }
    public int PageSize { get; set; }

    public WarehouseTrackingDetailSpecification(WarehouseTrackingDetailSpecParams specParams, int pageIndex, int pageSize)
        : base(w => 
            string.IsNullOrEmpty(specParams.Search) ||
            // Item name
            (!string.IsNullOrEmpty(w.ItemName) && w.ItemName.Contains(specParams.Search)) ||
            // Isbn
            (!string.IsNullOrEmpty(w.Isbn) && w.Isbn.Contains(specParams.Search)) ||
            // Unit price & Total amount
            (specParams.ParsedSearchDecimal.HasValue &&
             (w.UnitPrice == specParams.ParsedSearchDecimal.Value || w.TotalAmount == specParams.ParsedSearchDecimal.Value)) ||
            // Created at & Updated at
            (specParams.ParsedSearchDate.HasValue &&
             (w.CreatedAt.Date == specParams.ParsedSearchDate.Value ||
              (w.UpdatedAt.HasValue && w.UpdatedAt.Value.Date == specParams.ParsedSearchDate.Value)))
        )
    {
        // Pagination 
        PageIndex = pageIndex;
        PageSize = pageSize;
        
        // Enable split query
        EnableSplitQuery();
        
        // Filter has glue barcode
        if (specParams.HasGlueBarcode != null)
        {
            AddFilter(wd => wd.HasGlueBarcode == specParams.HasGlueBarcode);
            
            // Combine with filtering library item id
            AddFilter(wd => wd.LibraryItemId != null);
        }
        
        // Determine search type
        switch (specParams.SearchType)
        {
            case SearchType.BasicSearch:
                // Item name
                if (!string.IsNullOrEmpty(specParams.ItemName))
                {
                    AddFilter(w => w.ItemName.Contains(specParams.ItemName));    
                }
                
                // Item total
                if (specParams.ItemTotal != null && int.TryParse(specParams.ItemTotal.ToString(), out int itemTotal))
                {
                    AddFilter(w => w.ItemTotal == itemTotal);
                }
                
                // Isbn 
                if (!string.IsNullOrEmpty(specParams.Isbn))
                {
                    AddFilter(w => !string.IsNullOrEmpty(w.Isbn) && w.Isbn.Contains(specParams.Isbn));
                }
                
                // Unit price
                if (specParams.UnitPrice != null)
                {
                    AddFilter(w => w.UnitPrice == specParams.UnitPrice.Value);
                }
                
                // Total amount
                if (specParams.TotalAmount != null)
                {
                    AddFilter(w => w.TotalAmount == specParams.TotalAmount.Value);
                }
                
                // Stock transaction type
                if (specParams.StockTransactionType != null)
                {
                    AddFilter(w => w.StockTransactionType == specParams.StockTransactionType.Value);
                }
                
                break;
            case SearchType.AdvancedSearch:
                // Apply filters
                if (specParams.F != null && specParams.F.Any())
                {
                    // Convert to advanced filter list
                    var filerList = specParams.FromParamsToListAdvancedFilter();
                    if (filerList != null)
                    {
                        foreach (var filter in filerList)
                        {
                            // Item total
                            if (filter.FieldName.ToLowerInvariant() ==
                                nameof(WarehouseTrackingDetail.ItemTotal).ToLowerInvariant())
                            {
                                if (int.TryParse(filter.Value, out int parsedItemTotal))
                                {
                                    // Determine operator
                                    switch (filter.Operator)
                                    {
                                        case FilterOperator.Equals:
                                            AddFilter(w => w.ItemTotal == parsedItemTotal);
                                            break;
                                        case FilterOperator.NotEqualsTo:
                                            AddFilter(w => w.ItemTotal != parsedItemTotal);
                                            break;
                                        case FilterOperator.LessThan:
                                            AddFilter(w => w.ItemTotal < parsedItemTotal);
                                            break;
                                        case FilterOperator.LessThanOrEqualsTo:
                                            AddFilter(w => w.ItemTotal <= parsedItemTotal);
                                            break;
                                        case FilterOperator.GreaterThan:
                                            AddFilter(w => w.ItemTotal > parsedItemTotal);
                                            break;
                                        case FilterOperator.GreaterThanOrEqualsTo:
                                            AddFilter(w => w.ItemTotal >= parsedItemTotal);
                                            break;
                                    }
                                }
                            }
                            // Unit price
                            else if (filter.FieldName.ToLowerInvariant() ==
                                nameof(WarehouseTrackingDetail.UnitPrice).ToLowerInvariant())
                            {
                                if (decimal.TryParse(filter.Value, out decimal parsedUnitPrice))
                                {
                                    // Determine operator 
                                    switch (filter.Operator)
                                    {
                                        case FilterOperator.Equals:
                                            AddFilter(w => w.UnitPrice == parsedUnitPrice);
                                            break;
                                        case FilterOperator.NotEqualsTo:
                                            AddFilter(w => w.UnitPrice != parsedUnitPrice);
                                            break;
                                        case FilterOperator.LessThan:
                                            AddFilter(w => w.UnitPrice < parsedUnitPrice);
                                            break;
                                        case FilterOperator.LessThanOrEqualsTo:
                                            AddFilter(w => w.UnitPrice <= parsedUnitPrice);
                                            break;
                                        case FilterOperator.GreaterThan:
                                            AddFilter(w => w.UnitPrice > parsedUnitPrice);
                                            break;
                                        case FilterOperator.GreaterThanOrEqualsTo:
                                            AddFilter(w => w.UnitPrice >= parsedUnitPrice);
                                            break;
                                    }
                                }
                            }
                            // Total amount
                            else if (filter.FieldName.ToLowerInvariant() ==
                                     nameof(WarehouseTrackingDetail.TotalAmount).ToLowerInvariant())
                            {
                                if (decimal.TryParse(filter.Value, out decimal parsedTotalAmount))
                                {
                                    // Determine operator 
                                    switch (filter.Operator)
                                    {
                                        case FilterOperator.Equals:
                                            AddFilter(w => w.TotalAmount == parsedTotalAmount);
                                            break;
                                        case FilterOperator.NotEqualsTo:
                                            AddFilter(w => w.TotalAmount != parsedTotalAmount);
                                            break;
                                        case FilterOperator.LessThan:
                                            AddFilter(w => w.TotalAmount < parsedTotalAmount);
                                            break;
                                        case FilterOperator.LessThanOrEqualsTo:
                                            AddFilter(w => w.TotalAmount <= parsedTotalAmount);
                                            break;
                                        case FilterOperator.GreaterThan:
                                            AddFilter(w => w.TotalAmount > parsedTotalAmount);
                                            break;
                                        case FilterOperator.GreaterThanOrEqualsTo:
                                            AddFilter(w => w.TotalAmount >= parsedTotalAmount);
                                            break;
                                    }
                                }
                            }
                            // Isbn
                            else if (filter.FieldName.ToLowerInvariant() ==
                                     nameof(WarehouseTrackingDetail.Isbn).ToLowerInvariant())
                            {
                                var isbnList = filter.Value?.Split(",").Select(x => x.Trim()).ToList();
                                if (isbnList != null)
                                {
                                    // Initialize base spec to retrieve building filter when operator is 'includes'
                                    List<Expression<Func<WarehouseTrackingDetail, bool>>> includeExpressions = new();
                                    foreach (var isbn in isbnList)
                                    {
                                        // Determine operator
                                        switch (filter.Operator)
                                        {
                                            case FilterOperator.Includes:
                                                includeExpressions.Add(x => !string.IsNullOrEmpty(x.Isbn) &&
                                                    x.Isbn.ToLower() == isbn.ToLower());
                                                break;
                                            case FilterOperator.Equals:
                                                AddFilter(w => !string.IsNullOrEmpty(w.Isbn) &&
                                                    w.Isbn.ToLower() == isbn.ToLower());
                                                break;
                                            case FilterOperator.NotEqualsTo:
                                                AddFilter(w => !string.IsNullOrEmpty(w.Isbn) &&
                                                    w.Isbn.ToLower() != isbn.ToLower());
                                                break;
                                        }
                                    }
                                    
                                    if (includeExpressions.Any())
                                    {
                                        var resultExpression = includeExpressions.Skip(1).Aggregate(includeExpressions.FirstOrDefault(),
                                            (exp1, exp2) =>
                                            {
                                                if (exp1 != null)
                                                {
                                                    // Try to combined body of different expression with 'OR' operator
                                                    var body = Expression.OrElse(exp1.Body, Expression.Invoke(exp2, exp1.Parameters));
                                                    
                                                    // Return combined body
                                                    return Expression.Lambda<Func<WarehouseTrackingDetail, bool>>(body, exp1.Parameters);
                                                }
                
                                                return _ => false;
                                            });
                                        
                                        // Apply filter with 'includes'
                                        if(resultExpression != null) AddFilter(resultExpression);
                                    }
                                }
                            }
                            // Stock transaction type
                            else if (filter.FieldName.ToLowerInvariant() ==
                                     nameof(WarehouseTrackingDetail.StockTransactionType).ToLowerInvariant())
                            {
                                var enumList = filter.Value?
                                    .Split(",")
                                    .Select(x => x.Trim())
                                    .Select(x => Enum.TryParse(x, true, out StockTransactionType resultEnum) ? resultEnum : (StockTransactionType?)null)
                                    .ToList();
                                if (enumList != null)
                                {
                                    // Initialize base spec to retrieve building filter when operator is 'includes'
                                    List<Expression<Func<WarehouseTrackingDetail, bool>>> includeExpressions = new();
                                    foreach (var transactionType in enumList)
                                    {
                                        // Determine operator
                                        switch (filter.Operator)
                                        {
                                            case FilterOperator.Includes:
                                                includeExpressions.Add(w => transactionType != null &&
                                                    w.StockTransactionType == transactionType);
                                                break;
                                            case FilterOperator.Equals:
                                                AddFilter(w => transactionType != null &&
                                                    w.StockTransactionType == transactionType);
                                                break;
                                            case FilterOperator.NotEqualsTo:
                                                AddFilter(w => transactionType != null &&
                                                    w.StockTransactionType != transactionType);
                                                break;
                                        }
                                    }
                                    
                                    if (includeExpressions.Any())
                                    {
                                        var resultExpression = includeExpressions.Skip(1).Aggregate(includeExpressions.FirstOrDefault(),
                                            (exp1, exp2) =>
                                            {
                                                if (exp1 != null)
                                                {
                                                    // Try to combined body of different expression with 'OR' operator
                                                    var body = Expression.OrElse(exp1.Body, Expression.Invoke(exp2, exp1.Parameters));
                                                    
                                                    // Return combined body
                                                    return Expression.Lambda<Func<WarehouseTrackingDetail, bool>>(body, exp1.Parameters);
                                                }
                
                                                return _ => false;
                                            });
                                        
                                        // Apply filter with 'includes'
                                        if(resultExpression != null) AddFilter(resultExpression);
                                    }
                                }
                            }
                            // Category
                            else if (filter.FieldName.ToLowerInvariant() ==
                                     nameof(Category).ToLowerInvariant())
                            {
                                var categoryIds = filter.Value?.Split(",")
                                    .Select(x => x.Trim())
                                    .Select(x => int.TryParse(x, out var parsedId) ? parsedId : 0)
                                    .ToList();
                                if (categoryIds != null)
                                {
                                    // Initialize base spec to retrieve building filter when operator is 'includes'
                                    List<Expression<Func<WarehouseTrackingDetail, bool>>> includeExpressions = new();
                                    foreach (var categoryId in categoryIds)
                                    {
                                        // Determine operator
                                        switch (filter.Operator)
                                        {
                                            case FilterOperator.Includes:
                                                includeExpressions.Add(w => w.CategoryId == categoryId);
                                                break;
                                            case FilterOperator.Equals:
                                                AddFilter(w => w.CategoryId == categoryId);
                                                break;
                                            case FilterOperator.NotEqualsTo:
                                                AddFilter(w => w.CategoryId != categoryId);
                                                break;
                                        }
                                    }
                                    
                                    if (includeExpressions.Any())
                                    {
                                        var resultExpression = includeExpressions.Skip(1).Aggregate(includeExpressions.FirstOrDefault(),
                                            (exp1, exp2) =>
                                            {
                                                if (exp1 != null)
                                                {
                                                    // Try to combined body of different expression with 'OR' operator
                                                    var body = Expression.OrElse(exp1.Body, Expression.Invoke(exp2, exp1.Parameters));
                                                    
                                                    // Return combined body
                                                    return Expression.Lambda<Func<WarehouseTrackingDetail, bool>>(body, exp1.Parameters);
                                                }
                
                                                return _ => false;
                                            });
                                        
                                        // Apply filter with 'includes'
                                        if(resultExpression != null) AddFilter(resultExpression);
                                    }
                                }
                            }
                            // LibraryItemCondition
                            else if (filter.FieldName.ToLowerInvariant() ==
                                     nameof(LibraryItemCondition).ToLowerInvariant())
                            {
                                var conditionIds = filter.Value?.Split(",")
                                    .Select(x => x.Trim())
                                    .Select(x => int.TryParse(x, out var parsedId) ? parsedId : 0)
                                    .ToList();
                                if (conditionIds != null)
                                {
                                    // Initialize base spec to retrieve building filter when operator is 'includes'
                                    List<Expression<Func<WarehouseTrackingDetail, bool>>> includeExpressions = new();
                                    foreach (var conditionId in conditionIds)
                                    {
                                        // Determine operator
                                        switch (filter.Operator)
                                        {
                                            case FilterOperator.Includes:
                                                includeExpressions.Add(w => w.ConditionId == conditionId);
                                                break;
                                            case FilterOperator.Equals:
                                                AddFilter(w => w.ConditionId == conditionId);
                                                break;
                                            case FilterOperator.NotEqualsTo:
                                                AddFilter(w => w.ConditionId != conditionId);
                                                break;
                                        }
                                    }
                                    
                                    if (includeExpressions.Any())
                                    {
                                        var resultExpression = includeExpressions.Skip(1).Aggregate(includeExpressions.FirstOrDefault(),
                                            (exp1, exp2) =>
                                            {
                                                if (exp1 != null)
                                                {
                                                    // Try to combined body of different expression with 'OR' operator
                                                    var body = Expression.OrElse(exp1.Body, Expression.Invoke(exp2, exp1.Parameters));
                                                    
                                                    // Return combined body
                                                    return Expression.Lambda<Func<WarehouseTrackingDetail, bool>>(body, exp1.Parameters);
                                                }
                
                                                return _ => false;
                                            });
                                        
                                        // Apply filter with 'includes'
                                        if(resultExpression != null) AddFilter(resultExpression);
                                    }
                                }
                            }
                            // Created at
                            else if (filter.FieldName.ToLowerInvariant() ==
                                     nameof(LibraryItem.CreatedAt).ToLowerInvariant())
                            {
                                var dateRanges = filter.Value?
                                    .Split(',')
                                    .Select(str => str.Trim())
                                    .Select(str => DateTime.TryParseExact(
                                        str, 
                                        "yyyy-MM-dd", 
                                        null, 
                                        System.Globalization.DateTimeStyles.None, 
                                        out var validDate) ? validDate : (DateTime?)null)
                                    .ToList();
                                if (dateRanges!= null && dateRanges.Count == 2) // Only process date-range filtering 
                                {
                                    // Case 1: dateRanges[0] is not null && dateRanges[1] is not null
                                    if (dateRanges[0].HasValue && dateRanges[1].HasValue)
                                    {
                                        AddFilter(x => 
                                                       x.CreatedAt.Date >= dateRanges[0]!.Value.Date && 
                                                       x.CreatedAt.Date <= dateRanges[1]!.Value.Date);
                                    }
                                    // Case 2: dateRanges[0] is null && dateRanges[1] is not null
                                    else if (dateRanges[0] is null && dateRanges[1].HasValue)
                                    {
                                        AddFilter(x => x.CreatedAt.Date <= dateRanges[1]!.Value.Date);
                                    }
                                    // Case 3: dateRanges[0] is not null && dateRanges[1] is null
                                    else if (dateRanges[0].HasValue && dateRanges[1] is null)
                                    {
                                        AddFilter(x => x.CreatedAt.Date >= dateRanges[0]!.Value.Date);
                                    }
                                    else // Intentionally input 2 invalid value (null, null)
                                    {
                                        // Set default filter with false
                                        AddFilter(_ => false);
                                    }
                                }
                                else // Not exist any value
                                {
                                    // Set default filter with false
                                    AddFilter(_ => false);
                                }
                            }
                            // Updated at
                            else if (filter.FieldName.ToLowerInvariant() ==
                                     nameof(LibraryItem.UpdatedAt).ToLowerInvariant())
                            {
                                var dateRanges = filter.Value?
                                    .Split(',')
                                    .Select(str => str.Trim())
                                    .Select(str => DateTime.TryParseExact(
                                        str, 
                                        "yyyy-MM-dd", 
                                        null, 
                                        System.Globalization.DateTimeStyles.None, 
                                        out var validDate) ? validDate : (DateTime?)null)
                                    .ToList();
                                if (dateRanges!= null && dateRanges.Count == 2) // Only process date-range filtering 
                                {
                                    // Case 1: dateRanges[0] is not null && dateRanges[1] is not null
                                    if (dateRanges[0].HasValue && dateRanges[1].HasValue)
                                    {
                                        AddFilter(x => x.UpdatedAt.HasValue &&
                                                       x.UpdatedAt.Value.Date >= dateRanges[0]!.Value.Date && 
                                                       x.UpdatedAt.Value.Date <= dateRanges[1]!.Value.Date);
                                    }
                                    // Case 2: dateRanges[0] is null && dateRanges[1] is not null
                                    else if (dateRanges[0] is null && dateRanges[1].HasValue)
                                    {
                                        AddFilter(x => x.UpdatedAt.HasValue && 
                                                       x.UpdatedAt.Value.Date <= dateRanges[1]!.Value.Date);
                                    }
                                    // Case 3: dateRanges[0] is not null && dateRanges[1] is null
                                    else if (dateRanges[0].HasValue && dateRanges[1] is null)
                                    {
                                        AddFilter(x => x.UpdatedAt.HasValue && 
                                                       x.UpdatedAt.Value.Date >= dateRanges[0]!.Value.Date);
                                    }
                                    else // Intentionally input 2 invalid value (null, null)
                                    {
                                        // Set default filter with false
                                        AddFilter(_ => false);
                                    }
                                }
                                else // Not exist any value
                                {
                                    // Set default filter with false
                                    AddFilter(_ => false);
                                }
                            }
                        }
                    }
                }
                break;
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
            AddOrderByDescending(n => n.CreatedAt);
        }
    }
    
    private void ApplySorting(string propertyName, bool isDescending)
    {
        if (string.IsNullOrEmpty(propertyName)) return;

        // Initialize expression parameter with type of WarehouseTrackingDetail (x)
        var parameter = Expression.Parameter(typeof(WarehouseTrackingDetail), "x");
        // Assign property base on property name (x.PropertyName)
        var property = Expression.Property(parameter, propertyName);
        // Building a complete sort lambda expression (x => x.PropertyName)
        var sortExpression =
            Expression.Lambda<Func<WarehouseTrackingDetail, object>>(Expression.Convert(property, typeof(object)), parameter);

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

public static class WarehouseTrackingDetailSpecificationExtensions
{
    public static List<AdvancedFilter>? FromParamsToListAdvancedFilter(this WarehouseTrackingDetailSpecParams specParams)
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