using System.Linq.Expressions;
using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Specifications.Params;
using Microsoft.EntityFrameworkCore;

namespace FPTU_ELibrary.Domain.Specifications;

public class BorrowRecordSpecification : BaseSpecification<BorrowRecord>
{
    public int PageIndex { get; set; }
    public int PageSize { get; set; }
    
    public BorrowRecordSpecification(BorrowRecordSpecParams specParams, int pageIndex, int pageSize,
        string? email = null, Guid? userId = null)
        :base(br => 
            // Search with terms
            string.IsNullOrEmpty(specParams.Search) || 
            (
                // Email 
                br.LibraryCard.Users.Any(u => u.Email == specParams.Search) ||
                // Card barcode
                (!string.IsNullOrEmpty(br.LibraryCard.Barcode) && br.LibraryCard.Barcode.Contains(specParams.Search)) ||
                // Card fullname
                (!string.IsNullOrEmpty(br.LibraryCard.FullName) && br.LibraryCard.FullName.Contains(specParams.Search)) ||
                // BorrowRecordDetails
                // BorrowRecord -> BorrowRecordDetails
                br.BorrowRecordDetails.Any(brd => 
                    // Item title
                    !string.IsNullOrEmpty(brd.LibraryItemInstance.LibraryItem.Title) && brd.LibraryItemInstance.LibraryItem.Title.Contains(specParams.Search) ||
                    // Item ISBN
                    !string.IsNullOrEmpty(brd.LibraryItemInstance.LibraryItem.Isbn) && brd.LibraryItemInstance.LibraryItem.Isbn.Contains(specParams.Search) ||
                    // Item Cutter number
                    !string.IsNullOrEmpty(brd.LibraryItemInstance.LibraryItem.CutterNumber) && brd.LibraryItemInstance.LibraryItem.CutterNumber.Contains(specParams.Search) ||
                    // Item DDC
                    !string.IsNullOrEmpty(brd.LibraryItemInstance.LibraryItem.ClassificationNumber) && brd.LibraryItemInstance.LibraryItem.ClassificationNumber.Contains(specParams.Search) ||
                    // Item Genres
                    !string.IsNullOrEmpty(brd.LibraryItemInstance.LibraryItem.Genres) && brd.LibraryItemInstance.LibraryItem.Genres.Contains(specParams.Search) ||
                    // Item TopicalTerms
                    !string.IsNullOrEmpty(brd.LibraryItemInstance.LibraryItem.TopicalTerms) && brd.LibraryItemInstance.LibraryItem.TopicalTerms.Contains(specParams.Search)
                )
            ))
    {
        // Pagination
        PageIndex = pageIndex;
        PageSize = pageSize;
        
        // Enable split query
        EnableSplitQuery();
        
        // Add filter 
        if (!string.IsNullOrEmpty(email))
        {
            AddFilter(br => br.LibraryCard.Users.Any(u => u.Email == email));
        }
        if (userId.HasValue && userId != Guid.Empty)
        {
            AddFilter(br => br.LibraryCard.Users.Any(u => u.UserId == userId));
        }
        if (specParams.Status != null) // Status
        {
            AddFilter(br => br.BorrowRecordDetails.Any(brd => brd.Status == specParams.Status));
        }
        if (specParams.BorrowType != null) // Borrow type
        {
            AddFilter(br => br.BorrowType == specParams.BorrowType);
        }
        if (specParams.SelfServiceBorrow != null) // Is self-service borrow
        {
            AddFilter(br => br.SelfServiceBorrow == specParams.SelfServiceBorrow);
        }
        if (specParams.SelfServiceReturn != null) // Is self-service return
        {
            AddFilter(br => br.SelfServiceReturn == specParams.SelfServiceReturn);
        }
        if (specParams.BorrowDateRange != null
            && specParams.BorrowDateRange.Length > 1) // With range of borrow date
        {
            if (specParams.BorrowDateRange[0].HasValue && specParams.BorrowDateRange[1].HasValue)
            {
                AddFilter(x => x.BorrowDate.Date >= specParams.BorrowDateRange[0]!.Value.Date
                               && x.BorrowDate.Date <= specParams.BorrowDateRange[1]!.Value.Date);
            }
            else if ((specParams.BorrowDateRange[0] is null && specParams.BorrowDateRange[1].HasValue))
            {
                AddFilter(x => x.BorrowDate.Date <= specParams.BorrowDateRange[1]!.Value.Date);
            }
            else if (specParams.BorrowDateRange[0].HasValue && specParams.BorrowDateRange[1] is null)
            {
                AddFilter(x => x.BorrowDate.Date >= specParams.BorrowDateRange[0]!.Value.Date);
            }
        }
        if (specParams.DueDateRange != null
            && specParams.DueDateRange.Length > 1) // With range of borrow due date
        {
            if (specParams.DueDateRange[0].HasValue && specParams.DueDateRange[1].HasValue)
            {
                AddFilter(x => x.BorrowRecordDetails.Any(brd =>
                    brd.DueDate.Date >= specParams.DueDateRange[0]!.Value.Date &&
                    brd.DueDate.Date <= specParams.DueDateRange[1]!.Value.Date));
            }
            else if ((specParams.DueDateRange[0] is null && specParams.DueDateRange[1].HasValue))
            {
                AddFilter(x => x.BorrowRecordDetails.Any(brd =>
                    brd.DueDate.Date <= specParams.DueDateRange[1]!.Value.Date));
            }
            else if (specParams.DueDateRange[0].HasValue && specParams.DueDateRange[1] is null)
            {
                AddFilter(x => x.BorrowRecordDetails.Any(brd =>
                    brd.DueDate.Date >= specParams.DueDateRange[0]!.Value.Date));
            }
        }
        if (specParams.ReturnDateRange != null
            && specParams.ReturnDateRange.Length > 1) // With range of return date
        {
            if (specParams.ReturnDateRange[0].HasValue && specParams.ReturnDateRange[1].HasValue)
            {
                AddFilter(x => x.BorrowRecordDetails
                    .Any(brd => brd.ReturnDate!.Value.Date >= specParams.ReturnDateRange[0]!.Value.Date &&
                                brd.ReturnDate!.Value.Date <= specParams.ReturnDateRange[1]!.Value.Date));
            }
            else if ((specParams.ReturnDateRange[0] is null && specParams.ReturnDateRange[1].HasValue))
            {
                AddFilter(x => x.BorrowRecordDetails
                    .Any(brd => brd.ReturnDate!.Value.Date <= specParams.ReturnDateRange[1]!.Value.Date));
            }
            else if (specParams.ReturnDateRange[0].HasValue && specParams.ReturnDateRange[1] is null)
            {
                AddFilter(x => x.BorrowRecordDetails
                    .Any(brd => brd.ReturnDate!.Value.Date >= specParams.ReturnDateRange[0]!.Value.Date));
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
                    if (filter.FieldName.ToLowerInvariant() == nameof(LibraryItem.Title).ToLowerInvariant())
                    {
                        // Determine operator
                        switch (filter.Operator)
                        {
                            case FilterOperator.Includes:
                                AddFilter(br => br.BorrowRecordDetails.Any(brd => 
                                    brd.LibraryItemInstance.LibraryItem.Title.Contains(filter.Value ?? string.Empty)));
                                break;
                            case FilterOperator.Equals:
                                AddFilter(br => br.BorrowRecordDetails.Any(brd => 
                                    Equals(brd.LibraryItemInstance.LibraryItem.Title, filter.Value)));
                                break;
                            case FilterOperator.NotEqualsTo:
                                AddFilter(br => !br.BorrowRecordDetails.Any(brd => 
                                    Equals(brd.LibraryItemInstance.LibraryItem.Title, filter.Value)));
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
                                AddFilter(br => br.BorrowRecordDetails.Any(brd => 
                                    brd.LibraryItemInstance.LibraryItem.ClassificationNumber != null &&
                                    brd.LibraryItemInstance.LibraryItem.ClassificationNumber.Contains(filter.Value ?? string.Empty)));
                                break;
                            case FilterOperator.Equals:
                                AddFilter(br => br.BorrowRecordDetails.Any(brd => 
                                    brd.LibraryItemInstance.LibraryItem.ClassificationNumber != null &&
                                    Equals(brd.LibraryItemInstance.LibraryItem.ClassificationNumber, filter.Value)));
                                break;
                            case FilterOperator.NotEqualsTo:
                                AddFilter(br => !br.BorrowRecordDetails.Any(brd => 
                                    brd.LibraryItemInstance.LibraryItem.ClassificationNumber != null &&
                                    Equals(brd.LibraryItemInstance.LibraryItem.ClassificationNumber, filter.Value)));
                                break;
                        }
                    }
                    else if (filter.FieldName.ToLowerInvariant() == nameof(LibraryItem.CutterNumber).ToLowerInvariant())
                    {
                        // Determine operator
                        switch (filter.Operator)
                        {
                            case FilterOperator.Includes:
                                AddFilter(br => br.BorrowRecordDetails.Any(brd => 
                                    brd.LibraryItemInstance.LibraryItem.CutterNumber != null &&
                                    brd.LibraryItemInstance.LibraryItem.CutterNumber.Contains(filter.Value ?? string.Empty)));
                                break;
                            case FilterOperator.Equals:
                                AddFilter(br => br.BorrowRecordDetails.Any(brd => 
                                    brd.LibraryItemInstance.LibraryItem.CutterNumber != null &&
                                    Equals(brd.LibraryItemInstance.LibraryItem.CutterNumber, filter.Value)));
                                break;
                            case FilterOperator.NotEqualsTo:
                                AddFilter(br => !br.BorrowRecordDetails.Any(brd => 
                                    brd.LibraryItemInstance.LibraryItem.CutterNumber != null &&
                                    Equals(brd.LibraryItemInstance.LibraryItem.CutterNumber, filter.Value)));
                                break;
                        }
                    }
                    else if (filter.FieldName.ToLowerInvariant() == nameof(LibraryItem.Isbn).ToLowerInvariant())
                    {
                        var isbnList = filter.Value?.Split(",").Select(x => x.Trim()).ToList();
                        if (isbnList != null)
                        {
                            // Initialize base spec to retrieve building filter when operator is 'includes'
                            List<Expression<Func<BorrowRecord, bool>>> includeExpressions = new();
                            foreach (var isbn in isbnList)
                            {
                                // Determine operator
                                switch (filter.Operator)
                                {
                                    case FilterOperator.Includes:
                                        includeExpressions.Add(br => 
                                            string.IsNullOrEmpty(isbn) ||
                                            br.BorrowRecordDetails.Any(brd => brd.LibraryItemInstance.LibraryItem.Isbn != null && 
                                                Equals(brd.LibraryItemInstance.LibraryItem.Isbn.ToLower(), isbn.ToLower()))
                                        );
                                        break;
                                    case FilterOperator.Equals:
                                        AddFilter(br => 
                                            string.IsNullOrEmpty(isbn) || 
                                            br.BorrowRecordDetails.Any(brd => brd.LibraryItemInstance.LibraryItem.Isbn != null && 
                                                Equals(brd.LibraryItemInstance.LibraryItem.Isbn.ToLower(), isbn.ToLower())));
                                        break;
                                    case FilterOperator.NotEqualsTo:
                                        AddFilter(br => 
                                            string.IsNullOrEmpty(isbn) || 
                                            !br.BorrowRecordDetails.Any(brd => brd.LibraryItemInstance.LibraryItem.Isbn != null && 
                                                Equals(brd.LibraryItemInstance.LibraryItem.Isbn.ToLower(), isbn.ToLower())));
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
                                            return Expression.Lambda<Func<BorrowRecord, bool>>(body, exp1.Parameters);
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
                            List<Expression<Func<BorrowRecord, bool>>> includeExpressions = new();
                                // Determine operator
                                switch (filter.Operator)
                                {
                                    case FilterOperator.Includes:
                                        foreach (var genre in genres)
                                        {
                                            includeExpressions.Add(br => 
                                                string.IsNullOrEmpty(genre) ||
                                                br.BorrowRecordDetails.Any(brd => brd.LibraryItemInstance.LibraryItem.Genres != null && 
                                                    brd.LibraryItemInstance.LibraryItem.Genres.ToLower().Contains(genre.ToLower()))
                                            );
                                        }       
                                        break;
                                    case FilterOperator.Equals:
                                        AddFilter(br => 
                                            string.IsNullOrEmpty(filter.Value) || 
                                            br.BorrowRecordDetails.Any(brd => brd.LibraryItemInstance.LibraryItem.Genres != null && 
                                                Equals(brd.LibraryItemInstance.LibraryItem.Genres.ToLower(), filter.Value)));
                                        break;
                                    case FilterOperator.NotEqualsTo:
                                        AddFilter(br => 
                                            string.IsNullOrEmpty(filter.Value) || 
                                            !br.BorrowRecordDetails.Any(brd => brd.LibraryItemInstance.LibraryItem.Genres != null && 
                                                Equals(brd.LibraryItemInstance.LibraryItem.Genres.ToLower(), filter.Value)));
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
                                            return Expression.Lambda<Func<BorrowRecord, bool>>(body, exp1.Parameters);
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
                            List<Expression<Func<BorrowRecord, bool>>> includeExpressions = new();
                                // Determine operator
                                switch (filter.Operator)
                                {
                                    case FilterOperator.Includes:
                                        foreach (var term in topicalTerms)
                                        {
                                            includeExpressions.Add(br =>
                                                string.IsNullOrEmpty(term) ||
                                                br.BorrowRecordDetails.Any(brd =>
                                                    brd.LibraryItemInstance.LibraryItem.TopicalTerms != null &&
                                                    brd.LibraryItemInstance.LibraryItem.TopicalTerms.ToLower().Contains(term.ToLower()))
                                            );
                                        }
                                        break;
                                    case FilterOperator.Equals:
                                        AddFilter(br => 
                                            string.IsNullOrEmpty(filter.Value) || 
                                            br.BorrowRecordDetails.Any(brd => brd.LibraryItemInstance.LibraryItem.TopicalTerms != null && 
                                                Equals(brd.LibraryItemInstance.LibraryItem.TopicalTerms.ToLower(), filter.Value.ToLower())));
                                        break;
                                    case FilterOperator.NotEqualsTo:
                                        AddFilter(br => 
                                            string.IsNullOrEmpty(filter.Value) || 
                                            !br.BorrowRecordDetails.Any(brd => brd.LibraryItemInstance.LibraryItem.TopicalTerms != null && 
                                                Equals(brd.LibraryItemInstance.LibraryItem.TopicalTerms.ToLower(), filter.Value.ToLower())));
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
                                            return Expression.Lambda<Func<BorrowRecord, bool>>(body, exp1.Parameters);
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
                            List<Expression<Func<BorrowRecord, bool>>> includeExpressions = new();
                            foreach (var shelfNum in shelfNums)
                            {
                                // Determine operator
                                switch (filter.Operator)
                                {
                                    case FilterOperator.Includes:
                                        includeExpressions.Add(br => 
                                            string.IsNullOrEmpty(shelfNum) ||
                                            br.BorrowRecordDetails.Any(brd => brd.LibraryItemInstance.LibraryItem.Shelf != null && 
                                                Equals(brd.LibraryItemInstance.LibraryItem.Shelf.ShelfNumber.ToLower(), shelfNum.ToLower()))
                                        );
                                        break;
                                    case FilterOperator.Equals:
                                        AddFilter(br => 
                                            string.IsNullOrEmpty(shelfNum) || 
                                            br.BorrowRecordDetails.Any(brd => brd.LibraryItemInstance.LibraryItem.Shelf != null && 
                                                Equals(brd.LibraryItemInstance.LibraryItem.Shelf.ShelfNumber.ToLower(), shelfNum.ToLower())));
                                        break;
                                    case FilterOperator.NotEqualsTo:
                                        AddFilter(br => 
                                            string.IsNullOrEmpty(shelfNum) || 
                                            !br.BorrowRecordDetails.Any(brd => brd.LibraryItemInstance.LibraryItem.Shelf != null && 
                                                Equals(brd.LibraryItemInstance.LibraryItem.Shelf.ShelfNumber.ToLower(), shelfNum.ToLower())));
                                        break;
                                }
                            }
                            
                            if (includeExpressions.Any())
                            {
                                // br => br.BorrowRecordDetails.Any(brd => brd.LibraryItemInstance.LibraryItem.Shelf.ShelfNumber1)
                                // br => br.BorrowRecordDetails.Any(brd => brd.LibraryItemInstance.LibraryItem.Shelf.ShelfNumber2)
                                // br => br.BorrowRecordDetails.Any(brd => brd.LibraryItemInstance.LibraryItem.Shelf.ShelfNumber1) ||
                                //       br.BorrowRecordDetails.Any(brd => brd.LibraryItemInstance.LibraryItem.Shelf.ShelfNumber2)
                                var resultExpression = includeExpressions.Skip(1).Aggregate(includeExpressions.FirstOrDefault(),
                                    (exp1, exp2) =>
                                    {
                                        if (exp1 != null)
                                        {
                                            // Try to combined body of different expression with 'OR' operator
                                            var body = Expression.OrElse(exp1.Body, Expression.Invoke(exp2, exp1.Parameters));
                                            
                                            // Return combined body
                                            return Expression.Lambda<Func<BorrowRecord, bool>>(body, exp1.Parameters);
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
                            List<Expression<Func<BorrowRecord, bool>>> includeExpressions = new();
                            foreach (var categoryId in categoryIds)
                            {
                                // Try parse to integer
                                if (int.TryParse(categoryId, out var numCateId))
                                {
                                    // Determine operator
                                    switch (filter.Operator)
                                    {
                                        case FilterOperator.Includes:
                                            includeExpressions.Add(br => br.BorrowRecordDetails.Any(brd => 
                                                Equals(brd.LibraryItemInstance.LibraryItem.CategoryId, numCateId)));
                                            break;
                                        case FilterOperator.Equals:
                                            AddFilter(br => br.BorrowRecordDetails.Any(brd => 
                                                Equals(brd.LibraryItemInstance.LibraryItem.CategoryId, numCateId)));
                                            break;
                                        case FilterOperator.NotEqualsTo:
                                            AddFilter(br => !br.BorrowRecordDetails.Any(brd => 
                                                Equals(brd.LibraryItemInstance.LibraryItem.CategoryId, numCateId)));
                                            break;
                                    }
                                }
                            }
                            
                            if (includeExpressions.Any())
                            {
                                // br => br.BorrowRecordDetails.Any(brd => brd.LibraryItemInstance.LibraryItem.CategoryId1)
                                // br => br.BorrowRecordDetails.Any(brd => brd.LibraryItemInstance.LibraryItem.CategoryId2)
                                // br => br.BorrowRecordDetails.Any(brd => brd.LibraryItemInstance.LibraryItem.CategoryId1) ||
                                //       br.BorrowRecordDetails.Any(brd => brd.LibraryItemInstance.LibraryItem.CategoryId2)
                                var resultExpression = includeExpressions.Skip(1).Aggregate(includeExpressions.FirstOrDefault(),
                                    (exp1, exp2) =>
                                    {
                                        if (exp1 != null)
                                        {
                                            // Try to combined body of different expression with 'OR' operator
                                            var body = Expression.OrElse(exp1.Body, Expression.Invoke(exp2, exp1.Parameters));
                                            
                                            // Return combined body
                                            return Expression.Lambda<Func<BorrowRecord, bool>>(body, exp1.Parameters);
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
                            List<Expression<Func<BorrowRecord, bool>>> includeExpressions = new();
                            foreach (var barcode in barcodes)
                            {
                                // Determine operator
                                switch (filter.Operator)
                                {
                                    case FilterOperator.Includes:
                                        includeExpressions.Add(br => br.BorrowRecordDetails.Any(brd => 
                                            brd.LibraryItemInstance.LibraryItem.LibraryItemInstances.Any(li => 
                                                Equals(li.Barcode.ToLower(), barcode.ToLower()))));
                                        break;
                                    case FilterOperator.Equals:
                                        AddFilter(br => br.BorrowRecordDetails.Any(brd => 
                                            brd.LibraryItemInstance.LibraryItem.LibraryItemInstances.Any(li => 
                                                Equals(li.Barcode.ToLower(), barcode.ToLower()))));
                                        break;
                                    case FilterOperator.NotEqualsTo:
                                        AddFilter(br => !br.BorrowRecordDetails.Any(brd => 
                                            brd.LibraryItemInstance.LibraryItem.LibraryItemInstances.Any(li => 
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
                                            return Expression.Lambda<Func<BorrowRecord, bool>>(body, exp1.Parameters);
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
            AddOrderByDescending(n => n.BorrowDate);
        }
    }
    
    private void ApplySorting(string propertyName, bool isDescending)
    {
        if (string.IsNullOrEmpty(propertyName)) return;

        // Initialize expression parameter with type of LibraryItem (x)
        var parameter = Expression.Parameter(typeof(BorrowRecord), "x");
        // Assign property base on property name (x.PropertyName)
        var property = Expression.Property(parameter, propertyName);
        // Building a complete sort lambda expression (x => x.PropertyName)
        var sortExpression =
            Expression.Lambda<Func<BorrowRecord, object>>(Expression.Convert(property, typeof(object)), parameter);

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

public static class BorrowRecordExtensions
{
    public static List<AdvancedFilter>? FromParamsToListAdvancedFilter(this BorrowRecordSpecParams specParams)
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