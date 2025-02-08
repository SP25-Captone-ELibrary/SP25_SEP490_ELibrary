using System.Linq.Expressions;
using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Specifications.Params;
using Microsoft.EntityFrameworkCore;

namespace FPTU_ELibrary.Domain.Specifications;

public class BorrowRequestSpecification : BaseSpecification<BorrowRequest>
{
    public int PageIndex { get; set; }
    public int PageSize { get; set; }
    
    public BorrowRequestSpecification(BorrowRequestSpecParams specParams, int pageIndex, int pageSize, bool callFromManagement)
        :base(br => 
            // Search with terms
            string.IsNullOrEmpty(specParams.Search) || 
            (
                // Description
                (!string.IsNullOrEmpty(br.Description) && br.Description.Contains(specParams.Search)) ||
                // Cancellation reason
                (!string.IsNullOrEmpty(br.CancellationReason) && br.CancellationReason.Contains(specParams.Search)) ||
                // BorrowRequestDetails
                // BorrowRequest -> BorrowRequestDetails
                br.BorrowRequestDetails.Any(brd => 
                    // Item title
                    !string.IsNullOrEmpty(brd.LibraryItem.Title) && brd.LibraryItem.Title.Contains(specParams.Search) ||
                    // Item ISBN
                    !string.IsNullOrEmpty(brd.LibraryItem.Isbn) && brd.LibraryItem.Isbn.Contains(specParams.Search) ||
                    // Item Cutter number
                    !string.IsNullOrEmpty(brd.LibraryItem.CutterNumber) && brd.LibraryItem.CutterNumber.Contains(specParams.Search) ||
                    // Item DDC
                    !string.IsNullOrEmpty(brd.LibraryItem.ClassificationNumber) && brd.LibraryItem.ClassificationNumber.Contains(specParams.Search) ||
                    // Item Genres
                    !string.IsNullOrEmpty(brd.LibraryItem.Genres) && brd.LibraryItem.Genres.Contains(specParams.Search) ||
                    // Item TopicalTerms
                    !string.IsNullOrEmpty(brd.LibraryItem.TopicalTerms) && brd.LibraryItem.TopicalTerms.Contains(specParams.Search)
                )
            ))
    {
        // Pagination
        PageIndex = pageIndex;
        PageSize = pageSize;
        
        // Enable split query
        EnableSplitQuery();
        
        // Apply include 
        if (callFromManagement) // Management only
        {
            ApplyInclude(q => q
                .Include(br => br.BorrowRequestDetails)
                    .ThenInclude(brd => brd.LibraryItem)
                        .ThenInclude(li => li.Shelf)
                .Include(br => br.BorrowRequestDetails)
                    .ThenInclude(brd => brd.LibraryItem)
                        .ThenInclude(li => li.Category)
                .Include(br => br.BorrowRequestDetails)
                    .ThenInclude(brd => brd.LibraryItem)
                        .ThenInclude(li => li.LibraryItemInstances)
                .Include(br => br.LibraryCard)
            );
        }
        else // Default
        {
            ApplyInclude(q => q
                .Include(br => br.BorrowRequestDetails)
                .ThenInclude(brd => brd.LibraryItem)
            );
        }
        
        // Add filter 
        if (specParams.Status != null) // Status
        {
            AddFilter(br => br.Status == specParams.Status);
        }
        
        if (specParams.RequestDateRange != null
            && specParams.RequestDateRange.Length > 1) // With range of request date
        {
            if (specParams.RequestDateRange[0].HasValue && specParams.RequestDateRange[1].HasValue)
            {
                AddFilter(x => x.RequestDate.Date >= specParams.RequestDateRange[0]!.Value.Date
                               && x.RequestDate.Date <= specParams.RequestDateRange[1]!.Value.Date);
            }
            else if ((specParams.RequestDateRange[0] is null && specParams.RequestDateRange[1].HasValue))
            {
                AddFilter(x => x.RequestDate.Date <= specParams.RequestDateRange[1]!.Value.Date);
            }
            else if (specParams.RequestDateRange[0].HasValue && specParams.RequestDateRange[1] is null)
            {
                AddFilter(x => x.RequestDate.Date >= specParams.RequestDateRange[0]!.Value.Date);
            }
        }
        
        if (specParams.ExpirationDateRange != null
            && specParams.ExpirationDateRange.Length > 1) // With range of expiration date
        {
            if (specParams.ExpirationDateRange[0].HasValue && specParams.ExpirationDateRange[1].HasValue)
            {
                AddFilter(x => x.ExpirationDate.Date >= specParams.ExpirationDateRange[0]!.Value.Date
                               && x.ExpirationDate.Date <= specParams.ExpirationDateRange[1]!.Value.Date);
            }
            else if ((specParams.ExpirationDateRange[0] is null && specParams.ExpirationDateRange[1].HasValue))
            {
                AddFilter(x => x.ExpirationDate.Date <= specParams.ExpirationDateRange[1]!.Value.Date);
            }
            else if (specParams.ExpirationDateRange[0].HasValue && specParams.ExpirationDateRange[1] is null)
            {
                AddFilter(x => x.ExpirationDate.Date >= specParams.ExpirationDateRange[0]!.Value.Date);
            }
        }
        
        if (specParams.CancelledAtRange != null
            && specParams.CancelledAtRange.Length > 1) // With range of cancel date
        {
            if (specParams.CancelledAtRange[0].HasValue && specParams.CancelledAtRange[1].HasValue)
            {
                AddFilter(x => x.CancelledAt!.Value.Date >= specParams.CancelledAtRange[0]!.Value.Date
                               && x.CancelledAt!.Value.Date <= specParams.CancelledAtRange[1]!.Value.Date);
            }
            else if ((specParams.CancelledAtRange[0] is null && specParams.CancelledAtRange[1].HasValue))
            {
                AddFilter(x => x.CancelledAt!.Value.Date <= specParams.CancelledAtRange[1]!.Value.Date);
            }
            else if (specParams.CancelledAtRange[0].HasValue && specParams.CancelledAtRange[1] is null)
            {
                AddFilter(x => x.CancelledAt!.Value.Date >= specParams.CancelledAtRange[0]!.Value.Date);
            }
        }
        
        // Check whether call from management
        if (callFromManagement) // Only enable advanced filter for management
        {
            // Advanced filter
            if (specParams.F != null && specParams.F.Any())
            {
                // Convert to advanced filter list
                var filerList = specParams.FromParamsToListAdvancedFilter();
                if (filerList != null)
                {
                    foreach (var filter in filerList)
                    {
                        if (filter.FieldName.ToLowerInvariant() == nameof(LibraryItem.Title).ToLowerInvariant())
                        {
                            // Determine operator
                            switch (filter.Operator)
                            {
                                case FilterOperator.Includes:
                                    AddFilter(br => br.BorrowRequestDetails.Any(brd => 
                                        brd.LibraryItem.Title.Contains(filter.Value ?? string.Empty)));
                                    break;
                                case FilterOperator.Equals:
                                    AddFilter(br => br.BorrowRequestDetails.Any(brd => 
                                        Equals(brd.LibraryItem.Title, filter.Value)));
                                    break;
                                case FilterOperator.NotEqualsTo:
                                    AddFilter(br => !br.BorrowRequestDetails.Any(brd => 
                                        Equals(brd.LibraryItem.Title, filter.Value)));
                                    break;
                            }
                        }
                        else if (filter.FieldName.ToLowerInvariant() == nameof(LibraryCard).ToLowerInvariant())
                        {
                            // Determine operator
                            switch (filter.Operator)
                            {
                                case FilterOperator.Includes:
                                    AddFilter(br => br.LibraryCard.Barcode.Contains(filter.Value ?? string.Empty));
                                    break;
                                case FilterOperator.Equals:
                                    AddFilter(br => br.LibraryCard.Barcode.Equals(filter.Value));
                                    break;
                                case FilterOperator.NotEqualsTo:
                                    AddFilter(br => !br.LibraryCard.Barcode.Equals(filter.Value));
                                    break;
                            }
                        }
                        else if (filter.FieldName.ToLowerInvariant() == nameof(LibraryItem.ClassificationNumber).ToLowerInvariant())
                        {
                            // Determine operator
                            switch (filter.Operator)
                            {
                                case FilterOperator.Includes:
                                    AddFilter(br => br.BorrowRequestDetails.Any(brd => 
                                        brd.LibraryItem.ClassificationNumber != null &&
                                        brd.LibraryItem.ClassificationNumber.Contains(filter.Value ?? string.Empty)));
                                    break;
                                case FilterOperator.Equals:
                                    AddFilter(br => br.BorrowRequestDetails.Any(brd => 
                                        brd.LibraryItem.ClassificationNumber != null &&
                                        Equals(brd.LibraryItem.ClassificationNumber, filter.Value)));
                                    break;
                                case FilterOperator.NotEqualsTo:
                                    AddFilter(br => !br.BorrowRequestDetails.Any(brd => 
                                        brd.LibraryItem.ClassificationNumber != null &&
                                        Equals(brd.LibraryItem.ClassificationNumber, filter.Value)));
                                    break;
                            }
                        }
                        else if (filter.FieldName.ToLowerInvariant() == nameof(LibraryItem.CutterNumber).ToLowerInvariant())
                        {
                            // Determine operator
                            switch (filter.Operator)
                            {
                                case FilterOperator.Includes:
                                    AddFilter(br => br.BorrowRequestDetails.Any(brd => 
                                        brd.LibraryItem.CutterNumber != null &&
                                        brd.LibraryItem.CutterNumber.Contains(filter.Value ?? string.Empty)));
                                    break;
                                case FilterOperator.Equals:
                                    AddFilter(br => br.BorrowRequestDetails.Any(brd => 
                                        brd.LibraryItem.CutterNumber != null &&
                                        Equals(brd.LibraryItem.CutterNumber, filter.Value)));
                                    break;
                                case FilterOperator.NotEqualsTo:
                                    AddFilter(br => !br.BorrowRequestDetails.Any(brd => 
                                        brd.LibraryItem.CutterNumber != null &&
                                        Equals(brd.LibraryItem.CutterNumber, filter.Value)));
                                    break;
                            }
                        }
                        else if (filter.FieldName.ToLowerInvariant() == nameof(LibraryItem.Isbn).ToLowerInvariant())
                        {
                            var isbnList = filter.Value?.Split(",").Select(x => x.Trim()).ToList();
                            if (isbnList != null)
                            {
                                // Initialize base spec to retrieve building filter when operator is 'includes'
                                List<Expression<Func<BorrowRequest, bool>>> includeExpressions = new();
                                foreach (var isbn in isbnList)
                                {
                                    // Determine operator
                                    switch (filter.Operator)
                                    {
                                        case FilterOperator.Includes:
                                            includeExpressions.Add(br => 
                                                string.IsNullOrEmpty(isbn) ||
                                                br.BorrowRequestDetails.Any(brd => brd.LibraryItem.Isbn != null && 
                                                    Equals(brd.LibraryItem.Isbn.ToLower(), isbn.ToLower()))
                                            );
                                            break;
                                        case FilterOperator.Equals:
                                            AddFilter(br => 
                                                string.IsNullOrEmpty(isbn) || 
                                                br.BorrowRequestDetails.Any(brd => brd.LibraryItem.Isbn != null && 
                                                    Equals(brd.LibraryItem.Isbn.ToLower(), isbn.ToLower())));
                                            break;
                                        case FilterOperator.NotEqualsTo:
                                            AddFilter(br => 
                                                string.IsNullOrEmpty(isbn) || 
                                                !br.BorrowRequestDetails.Any(brd => brd.LibraryItem.Isbn != null && 
                                                    Equals(brd.LibraryItem.Isbn.ToLower(), isbn.ToLower())));
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
                                                return Expression.Lambda<Func<BorrowRequest, bool>>(body, exp1.Parameters);
                                            }
            
                                            return _ => false;
                                        });
                                    
                                    // Apply filter with 'includes'
                                    if(resultExpression != null) AddFilter(resultExpression);
                                }
                            }
                        }
                        else if (filter.FieldName.ToLowerInvariant() == nameof(LibraryItem.Genres).ToLowerInvariant())
                        {
                            var genres = filter.Value?.Split(",").Select(x => x.Trim()).ToList();
                            if (genres != null)
                            {
                                // Initialize base spec to retrieve building filter when operator is 'includes'
                                List<Expression<Func<BorrowRequest, bool>>> includeExpressions = new();
                                    // Determine operator
                                    switch (filter.Operator)
                                    {
                                        case FilterOperator.Includes:
                                            foreach (var genre in genres)
                                            {
                                                includeExpressions.Add(br => 
                                                    string.IsNullOrEmpty(genre) ||
                                                    br.BorrowRequestDetails.Any(brd => brd.LibraryItem.Genres != null && 
                                                        brd.LibraryItem.Genres.ToLower().Contains(genre.ToLower()))
                                                );
                                            }       
                                            break;
                                        case FilterOperator.Equals:
                                            AddFilter(br => 
                                                string.IsNullOrEmpty(filter.Value) || 
                                                br.BorrowRequestDetails.Any(brd => brd.LibraryItem.Genres != null && 
                                                    Equals(brd.LibraryItem.Genres.ToLower(), filter.Value)));
                                            break;
                                        case FilterOperator.NotEqualsTo:
                                            AddFilter(br => 
                                                string.IsNullOrEmpty(filter.Value) || 
                                                !br.BorrowRequestDetails.Any(brd => brd.LibraryItem.Genres != null && 
                                                    Equals(brd.LibraryItem.Genres.ToLower(), filter.Value)));
                                            break;
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
                                                return Expression.Lambda<Func<BorrowRequest, bool>>(body, exp1.Parameters);
                                            }
            
                                            return _ => false;
                                        });
                                    
                                    // Apply filter with 'includes'
                                    if(resultExpression != null) AddFilter(resultExpression);
                                }
                            }
                        }
                        else if (filter.FieldName.ToLowerInvariant() == nameof(LibraryItem.TopicalTerms).ToLowerInvariant())
                        {
                            var topicalTerms = filter.Value?.Split(",").Select(x => x.Trim()).ToList();
                            if (topicalTerms != null)
                            {
                                // Initialize base spec to retrieve building filter when operator is 'includes'
                                List<Expression<Func<BorrowRequest, bool>>> includeExpressions = new();
                                    // Determine operator
                                    switch (filter.Operator)
                                    {
                                        case FilterOperator.Includes:
                                            foreach (var term in topicalTerms)
                                            {
                                                includeExpressions.Add(br =>
                                                    string.IsNullOrEmpty(term) ||
                                                    br.BorrowRequestDetails.Any(brd =>
                                                        brd.LibraryItem.TopicalTerms != null &&
                                                        brd.LibraryItem.TopicalTerms.ToLower().Contains(term.ToLower()))
                                                );
                                            }
                                            break;
                                        case FilterOperator.Equals:
                                            AddFilter(br => 
                                                string.IsNullOrEmpty(filter.Value) || 
                                                br.BorrowRequestDetails.Any(brd => brd.LibraryItem.TopicalTerms != null && 
                                                    Equals(brd.LibraryItem.TopicalTerms.ToLower(), filter.Value.ToLower())));
                                            break;
                                        case FilterOperator.NotEqualsTo:
                                            AddFilter(br => 
                                                string.IsNullOrEmpty(filter.Value) || 
                                                !br.BorrowRequestDetails.Any(brd => brd.LibraryItem.TopicalTerms != null && 
                                                    Equals(brd.LibraryItem.TopicalTerms.ToLower(), filter.Value.ToLower())));
                                            break;
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
                                                return Expression.Lambda<Func<BorrowRequest, bool>>(body, exp1.Parameters);
                                            }
            
                                            return _ => false;
                                        });
                                    
                                    // Apply filter with 'includes'
                                    if(resultExpression != null) AddFilter(resultExpression);
                                }
                            }
                        }
                        else if (filter.FieldName.ToLowerInvariant() == nameof(LibraryShelf.ShelfNumber).ToLowerInvariant())
                        {
                            var shelfNums = filter.Value?.Split(",").Select(x => x.Trim()).ToList();
                            if (shelfNums != null)
                            {
                                // Initialize base spec to retrieve building filter when operator is 'includes'
                                List<Expression<Func<BorrowRequest, bool>>> includeExpressions = new();
                                foreach (var shelfNum in shelfNums)
                                {
                                    // Determine operator
                                    switch (filter.Operator)
                                    {
                                        case FilterOperator.Includes:
                                            includeExpressions.Add(br => 
                                                string.IsNullOrEmpty(shelfNum) ||
                                                br.BorrowRequestDetails.Any(brd => brd.LibraryItem.Shelf != null && 
                                                    Equals(brd.LibraryItem.Shelf.ShelfNumber.ToLower(), shelfNum.ToLower()))
                                            );
                                            break;
                                        case FilterOperator.Equals:
                                            AddFilter(br => 
                                                string.IsNullOrEmpty(shelfNum) || 
                                                br.BorrowRequestDetails.Any(brd => brd.LibraryItem.Shelf != null && 
                                                    Equals(brd.LibraryItem.Shelf.ShelfNumber.ToLower(), shelfNum.ToLower())));
                                            break;
                                        case FilterOperator.NotEqualsTo:
                                            AddFilter(br => 
                                                string.IsNullOrEmpty(shelfNum) || 
                                                !br.BorrowRequestDetails.Any(brd => brd.LibraryItem.Shelf != null && 
                                                    Equals(brd.LibraryItem.Shelf.ShelfNumber.ToLower(), shelfNum.ToLower())));
                                            break;
                                    }
                                }
                                
                                if (includeExpressions.Any())
                                {
                                    // br => br.BorrowRequestDetails.Any(brd => brd.LibraryItem.Shelf.ShelfNumber1)
                                    // br => br.BorrowRequestDetails.Any(brd => brd.LibraryItem.Shelf.ShelfNumber2)
                                    // br => br.BorrowRequestDetails.Any(brd => brd.LibraryItem.Shelf.ShelfNumber1) ||
                                    //       br.BorrowRequestDetails.Any(brd => brd.LibraryItem.Shelf.ShelfNumber2)
                                    var resultExpression = includeExpressions.Skip(1).Aggregate(includeExpressions.FirstOrDefault(),
                                        (exp1, exp2) =>
                                        {
                                            if (exp1 != null)
                                            {
                                                // Try to combined body of different expression with 'OR' operator
                                                var body = Expression.OrElse(exp1.Body, Expression.Invoke(exp2, exp1.Parameters));
                                                
                                                // Return combined body
                                                return Expression.Lambda<Func<BorrowRequest, bool>>(body, exp1.Parameters);
                                            }
            
                                            return _ => false;
                                        });
                                    
                                    // Apply filter with 'includes'
                                    if(resultExpression != null) AddFilter(resultExpression);
                                }
                            }
                        }
                        else if (filter.FieldName.ToLowerInvariant() == nameof(Category).ToLowerInvariant())
                        {
                            var categoryIds = filter.Value?.Split(",").Select(x => x.Trim()).ToList();
                            if (categoryIds != null)
                            {
                                // Initialize base spec to retrieve building filter when operator is 'includes'
                                List<Expression<Func<BorrowRequest, bool>>> includeExpressions = new();
                                foreach (var categoryId in categoryIds)
                                {
                                    // Try parse to integer
                                    if (int.TryParse(categoryId, out var numCateId))
                                    {
                                        // Determine operator
                                        switch (filter.Operator)
                                        {
                                            case FilterOperator.Includes:
                                                includeExpressions.Add(br => br.BorrowRequestDetails.Any(brd => 
                                                    Equals(brd.LibraryItem.CategoryId, numCateId)));
                                                break;
                                            case FilterOperator.Equals:
                                                AddFilter(br => br.BorrowRequestDetails.Any(brd => 
                                                    Equals(brd.LibraryItem.CategoryId, numCateId)));
                                                break;
                                            case FilterOperator.NotEqualsTo:
                                                AddFilter(br => !br.BorrowRequestDetails.Any(brd => 
                                                    Equals(brd.LibraryItem.CategoryId, numCateId)));
                                                break;
                                        }
                                    }
                                }
                                
                                if (includeExpressions.Any())
                                {
                                    // br => br.BorrowRequestDetails.Any(brd => brd.LibraryItem.CategoryId1)
                                    // br => br.BorrowRequestDetails.Any(brd => brd.LibraryItem.CategoryId2)
                                    // br => br.BorrowRequestDetails.Any(brd => brd.LibraryItem.CategoryId1) ||
                                    //       br.BorrowRequestDetails.Any(brd => brd.LibraryItem.CategoryId2)
                                    var resultExpression = includeExpressions.Skip(1).Aggregate(includeExpressions.FirstOrDefault(),
                                        (exp1, exp2) =>
                                        {
                                            if (exp1 != null)
                                            {
                                                // Try to combined body of different expression with 'OR' operator
                                                var body = Expression.OrElse(exp1.Body, Expression.Invoke(exp2, exp1.Parameters));
                                                
                                                // Return combined body
                                                return Expression.Lambda<Func<BorrowRequest, bool>>(body, exp1.Parameters);
                                            }
            
                                            return _ => false;
                                        });
                                    
                                    // Apply filter with 'includes'
                                    if(resultExpression != null) AddFilter(resultExpression);
                                }
                            }
                        }
                        else if (filter.FieldName.ToLowerInvariant() == nameof(LibraryItemInstance.Barcode).ToLowerInvariant())
                        {
                            var barcodes = filter.Value?.Split(",").Select(x => x.Trim()).ToList();
                            if (barcodes != null)
                            {
                                // Initialize base spec to retrieve building filter when operator is 'includes'
                                List<Expression<Func<BorrowRequest, bool>>> includeExpressions = new();
                                foreach (var barcode in barcodes)
                                {
                                    // Determine operator
                                    switch (filter.Operator)
                                    {
                                        case FilterOperator.Includes:
                                            includeExpressions.Add(br => br.BorrowRequestDetails.Any(brd => 
                                                brd.LibraryItem.LibraryItemInstances.Any(li => 
                                                    Equals(li.Barcode.ToLower(), barcode.ToLower()))));
                                            break;
                                        case FilterOperator.Equals:
                                            AddFilter(br => br.BorrowRequestDetails.Any(brd => 
                                                brd.LibraryItem.LibraryItemInstances.Any(li => 
                                                    Equals(li.Barcode.ToLower(), barcode.ToLower()))));
                                            break;
                                        case FilterOperator.NotEqualsTo:
                                            AddFilter(br => !br.BorrowRequestDetails.Any(brd => 
                                                brd.LibraryItem.LibraryItemInstances.Any(li => 
                                                    Equals(li.Barcode.ToLower(), barcode.ToLower()))));
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
                                                return Expression.Lambda<Func<BorrowRequest, bool>>(body, exp1.Parameters);
                                            }
            
                                            return _ => false;
                                        });
                                    
                                    // Apply filter with 'includes'
                                    if(resultExpression != null) AddFilter(resultExpression);
                                }
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
            AddOrderByDescending(n => n.RequestDate);
        }
    }
    
    private void ApplySorting(string propertyName, bool isDescending)
    {
        if (string.IsNullOrEmpty(propertyName)) return;

        // Initialize expression parameter with type of LibraryItem (x)
        var parameter = Expression.Parameter(typeof(BorrowRequest), "x");
        // Assign property base on property name (x.PropertyName)
        var property = Expression.Property(parameter, propertyName);
        // Building a complete sort lambda expression (x => x.PropertyName)
        var sortExpression =
            Expression.Lambda<Func<BorrowRequest, object>>(Expression.Convert(property, typeof(object)), parameter);

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

public static class BorrowRequestExtensions
{
    public static List<AdvancedFilter>? FromParamsToListAdvancedFilter(this BorrowRequestSpecParams specParams)
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