using System.Linq.Expressions;
using FPTU_ELibrary.Domain.Common.Enums;
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
                (!string.IsNullOrEmpty(be.Book.BookCode) && be.Book.BookCode.Contains(specParams.Search)) ||
                (!string.IsNullOrEmpty(be.Book.Title) && be.Book.Title.Contains(specParams.Search)) ||
                (!string.IsNullOrEmpty(be.Book.SubTitle) && be.Book.SubTitle.Contains(specParams.Search)) ||
                (!string.IsNullOrEmpty(be.Book.Summary) && be.Book.Summary.Contains(specParams.Search)) ||
                // BookCategories
                be.Book.BookCategories.Any(bc =>
                    !string.IsNullOrEmpty(bc.Category.EnglishName) &&
                    bc.Category.EnglishName.Contains(specParams.Search) ||
                    !string.IsNullOrEmpty(bc.Category.VietnameseName) &&
                    bc.Category.VietnameseName.Contains(specParams.Search)
                ) ||
                // BookEditionAuthors
                // BookEditions -> BookEditionAuthors
                be.BookEditionAuthors.Any(a =>
                    !string.IsNullOrEmpty(a.Author.AuthorCode) && a.Author.AuthorCode.Contains(specParams.Search) ||
                    !string.IsNullOrEmpty(a.Author.FullName) && a.Author.FullName.Contains(specParams.Search) ||
                    !string.IsNullOrEmpty(a.Author.Biography) && a.Author.Biography.Contains(specParams.Search) ||
                    !string.IsNullOrEmpty(a.Author.Nationality) && a.Author.Nationality.Contains(specParams.Search)
                ) ||
                // BookEditionCopies
                // BookEditions -> BookEditionCopies
                be.BookEditionCopies.Any(bec =>
                    !string.IsNullOrEmpty(bec.Barcode) && bec.Barcode.Contains(specParams.Search) ||
                    !string.IsNullOrEmpty(bec.Code) && bec.Code.Contains(specParams.Search)
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
                .ThenInclude(e => e.Category)
            .Include(e => e.BookEditionAuthors)
                .ThenInclude(e => e.Author)
            .Include(b => b.BookEditionCopies)
        );

        // Default order by 
        AddOrderBy(e => e.Book.Title);

        // Boolean filters
        if (specParams.CanBorrow != null) // Can borrow status
        {
            AddFilter(x => x.CanBorrow == specParams.CanBorrow);
        }
        if (specParams.IsDeleted != null) // Is deleted
        {
            AddFilter(x => x.IsDeleted == specParams.IsDeleted);       
        }
        if (specParams.IsTrained != null) // Is trained
        {
            AddFilter(x => x.IsTrained == specParams.IsTrained);       
        }
        
        // Apply filters 
        if (specParams.F != null && specParams.F.Any())
        {
            // Convert to advanced filter list
            var filerList = specParams.FromParamsToListAdvancedFilter();
            if (filerList != null)
            {
                foreach (var filter in filerList)
                {
                    // Handling for properties, which in navigations or reference
                    if (filter.FieldName.ToLowerInvariant() == nameof(Author).ToLowerInvariant())
                    {
                        // Determine operator
                        switch (filter.Operator)
                        {
                            case FilterOperator.Includes:
                                AddFilter(be => be.BookEditionAuthors.Any(bea => 
                                    bea.Author.FullName.Contains(filter.Value ?? string.Empty)));
                                break;
                            case FilterOperator.Equals:
                                AddFilter(be => be.BookEditionAuthors.Any(bea => 
                                    Equals(bea.Author.FullName, filter.Value)));
                                break;
                            case FilterOperator.NotEqualsTo:
                                AddFilter(be => be.BookEditionAuthors.Any(bea => 
                                    !Equals(bea.Author.FullName, filter.Value)));
                                break;
                        }
                    }
                    else if (filter.FieldName.ToLowerInvariant() == nameof(LibraryShelf.ShelfNumber).ToLowerInvariant())
                    {
                        var shelfNums = filter.Value?.Split(",").Select(x => x.Trim()).ToList();
                        if (shelfNums != null)
                        {
                            // Initialize base spec to retrieve building filter when operator is 'includes'
                            List<Expression<Func<BookEdition, bool>>> includeExpressions = new();
                            foreach (var shelfNum in shelfNums)
                            {
                                // Determine operator
                                switch (filter.Operator)
                                {
                                    case FilterOperator.Includes:
                                        includeExpressions.Add(be => 
                                            string.IsNullOrEmpty(shelfNum) ||
                                            (be.Shelf != null && Equals(be.Shelf.ShelfNumber.ToLower(), shelfNum.ToLower()))
                                        );
                                        break;
                                    case FilterOperator.Equals:
                                        AddFilter(be => 
                                            string.IsNullOrEmpty(shelfNum) || 
                                            (be.Shelf != null && Equals(be.Shelf.ShelfNumber.ToLower(), shelfNum.ToLower())));
                                        break;
                                    case FilterOperator.NotEqualsTo:
                                        AddFilter(be => 
                                            string.IsNullOrEmpty(shelfNum) || 
                                            (be.Shelf != null && !Equals(be.Shelf.ShelfNumber.ToLower(), shelfNum.ToLower())));
                                        break;
                                }
                            }
                            
                            if (includeExpressions.Any())
                            {
                                // be => be.Book.BookCategories.Any1()
                                // be => be.Book.BookCategories.Any2()
                                // be => be.Book.BookCategories.Any1() || be.Book.BookCategories.Any2()
                                var resultExpression = includeExpressions.Skip(1).Aggregate(includeExpressions.FirstOrDefault(),
                                    (exp1, exp2) =>
                                    {
                                        if (exp1 != null)
                                        {
                                            // Try to combined body of different expression with 'OR' operator
                                            var body = Expression.OrElse(exp1.Body, Expression.Invoke(exp2, exp1.Parameters));
                                            
                                            // Return combined body
                                            return Expression.Lambda<Func<BookEdition, bool>>(body, exp1.Parameters);
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
                            List<Expression<Func<BookEdition, bool>>> includeExpressions = new();
                            foreach (var categoryId in categoryIds)
                            {
                                // Try parse to integer
                                if (int.TryParse(categoryId, out var numCateId))
                                {
                                    // Determine operator
                                    switch (filter.Operator)
                                    {
                                        case FilterOperator.Includes:
                                            includeExpressions.Add(be => be.Book.BookCategories.Any(x =>
                                                Equals(x.CategoryId, numCateId)));
                                            break;
                                        case FilterOperator.Equals:
                                            AddFilter(be => be.Book.BookCategories.Any(x =>
                                                Equals(x.CategoryId, numCateId)));
                                            break;
                                        case FilterOperator.NotEqualsTo:
                                            AddFilter(be => be.Book.BookCategories.All(x =>
                                                !Equals(x.CategoryId, numCateId)));
                                            break;
                                    }
                                }
                            }
                            
                            if (includeExpressions.Any())
                            {
                                // be => be.Book.BookCategories.Any1()
                                // be => be.Book.BookCategories.Any2()
                                // be => be.Book.BookCategories.Any1() || be.Book.BookCategories.Any2()
                                var resultExpression = includeExpressions.Skip(1).Aggregate(includeExpressions.FirstOrDefault(),
                                    (exp1, exp2) =>
                                    {
                                        if (exp1 != null)
                                        {
                                            // Try to combined body of different expression with 'OR' operator
                                            var body = Expression.OrElse(exp1.Body, Expression.Invoke(exp2, exp1.Parameters));
                                            
                                            // Return combined body
                                            return Expression.Lambda<Func<BookEdition, bool>>(body, exp1.Parameters);
                                        }

                                        return _ => false;
                                    });
                                
                                // Apply filter with 'includes'
                                if(resultExpression != null) AddFilter(resultExpression);
                            }
                        }
                    }
                    else if (filter.FieldName.ToLowerInvariant() == nameof(BookEditionCopy.Barcode).ToLowerInvariant())
                    {
                        var barcodes = filter.Value?.Split(",").Select(x => x.Trim()).ToList();
                        if (barcodes != null)
                        {
                            // Initialize base spec to retrieve building filter when operator is 'includes'
                            List<Expression<Func<BookEdition, bool>>> includeExpressions = new();
                            foreach (var barcode in barcodes)
                            {
                                // Determine operator
                                switch (filter.Operator)
                                {
                                    case FilterOperator.Includes:
                                        includeExpressions.Add(be => be.BookEditionCopies.Any(x => 
                                            Equals(x.Barcode.ToLower(), barcode.ToLower())));
                                        break;
                                    case FilterOperator.Equals:
                                        AddFilter(be => be.BookEditionCopies.Any(x => 
                                            Equals(x.Barcode.ToLower(), barcode.ToLower())));
                                        break;
                                    case FilterOperator.NotEqualsTo:
                                        AddFilter(be => be.BookEditionCopies.All(x =>
                                            !Equals(x.Barcode.ToLower(), barcode.ToLower())));
                                        break;
                                }
                            }

                            if (includeExpressions.Any())
                            {
                                // be => be.BookEditionCopies.Any1()
                                // be => be.BookEditionCopies.Any2()
                                // be => be.BookEditionCopies.Any1() || be.BookEditionCopies.Any2()
                                var resultExpression = includeExpressions.Skip(1).Aggregate(includeExpressions.FirstOrDefault(),
                                    (exp1, exp2) =>
                                    {
                                        if (exp1 != null)
                                        {
                                            // Try to combined body of different expression with 'OR' operator
                                            var body = Expression.OrElse(exp1.Body, Expression.Invoke(exp2, exp1.Parameters));
                                            
                                            // Return combined body
                                            return Expression.Lambda<Func<BookEdition, bool>>(body, exp1.Parameters);
                                        }

                                        return _ => false;
                                    });
                                
                                // Apply filter with 'includes'
                                if(resultExpression != null) AddFilter(resultExpression);
                            }
                        }
                    }
                    // Handle for properties, which in root entity
                    else if (filter.FieldName.ToLowerInvariant() == 
                             nameof(BookEdition.Status).ToLowerInvariant())
                    {
                        if (Enum.Parse(typeof(BookEditionStatus), filter.Value ?? string.Empty) is BookEditionStatus status)
                        {
                            // Determine operator 
                            switch (filter.Operator)
                            {
                                case FilterOperator.Equals:
                                    AddFilter(be => Equals(be.Status, status));
                                    break;
                                case FilterOperator.NotEqualsTo:
                                    AddFilter(be => !Equals(be.Status, status));
                                    break;
                            }
                        }
                    }
                    else if (filter.FieldName.ToLowerInvariant() ==
                             nameof(BookEdition.CreatedAt).ToLowerInvariant())
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
                    else if (filter.FieldName.ToLowerInvariant() ==
                             nameof(BookEdition.UpdatedAt).ToLowerInvariant())
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
                    else if (filter.FieldName.ToLowerInvariant() ==
                             nameof(BookEdition.TrainedDay).ToLowerInvariant())
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
                                AddFilter(x => x.TrainedDay.HasValue &&
                                               x.TrainedDay.Value.Date >= dateRanges[0]!.Value.Date && 
                                               x.TrainedDay.Value.Date <= dateRanges[1]!.Value.Date);
                            }
                            // Case 2: dateRanges[0] is null && dateRanges[1] is not null
                            else if (dateRanges[0] is null && dateRanges[1].HasValue)
                            {
                                AddFilter(x => x.TrainedDay.HasValue && 
                                               x.TrainedDay.Value.Date <= dateRanges[1]!.Value.Date);
                            }
                            // Case 3: dateRanges[0] is not null && dateRanges[1] is null
                            else if (dateRanges[0].HasValue && dateRanges[1] is null)
                            {
                                AddFilter(x => x.TrainedDay.HasValue && 
                                               x.TrainedDay.Value.Date >= dateRanges[0]!.Value.Date);
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
                    else
                    {
                        switch (filter.Operator)
                        {
                            case FilterOperator.Includes:
                                AddFilter(CreateContainsFilter(filter.FieldName, filter.Value));
                                break;
                            case FilterOperator.Equals:
                                AddFilter(CreateEqualityFilter(filter.FieldName, filter.Value));
                                break;
                            case FilterOperator.NotEqualsTo:
                                AddFilter(CreateInEqualityFilter(filter.FieldName, filter.Value));
                                break;
                            case FilterOperator.LessThan:
                                AddFilter(CreateComparisonFilter(filter.FieldName, filter.Value, filter.Operator));
                                break;
                            case FilterOperator.LessThanOrEqualsTo:
                                AddFilter(CreateComparisonFilter(filter.FieldName, filter.Value, filter.Operator));
                                break;
                            case FilterOperator.GreaterThan:
                                AddFilter(CreateComparisonFilter(filter.FieldName, filter.Value, filter.Operator));
                                break;
                            case FilterOperator.GreaterThanOrEqualsTo:
                                AddFilter(CreateComparisonFilter(filter.FieldName, filter.Value, filter.Operator));
                                break;
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
                { "BORROWEDCOPIES", x => x.BookEditionInventory!.BorrowedCopies },
                { "RESERVEDCOPIES", x => x.BookEditionInventory!.ReservedCopies },
                { "AUTHOR", x => x.BookEditionAuthors.Select(bea => bea.Author.FullName) },
                { "TITLE", x => x.Book.Title },
                { "BOOKCODE", x => x.Book.BookCode },
                { "EDITIONTITLE", x => x.EditionTitle! },
                { "SHELF", x => x.Shelf != null ? x.Shelf.ShelfNumber : null! },
                { "FORMAT", x => x.Format ?? null! },
                { "ISBN", x => x.Isbn },
                { "LANGUAGE", x => x.Language },
                { "COVERIMAGE", x => x.CoverImage ?? null! },
                { "PUBLISHER", x => x.Publisher ?? null! },
                { "ESTIMATEDPRICE", x => x.EstimatedPrice},
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

    /// <summary>
    /// Text-based contains filtering 
    /// </summary>
    /// <param name="fieldName"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    private static Expression<Func<BookEdition, bool>> CreateContainsFilter(
        string fieldName, string? value)
    {
        try
        {
            // Create ParameterExpression node with the specified name and type
            var parameter = Expression.Parameter(typeof(BookEdition));
            
            // Split the field name to navigate nested properties 
            var fields = fieldName.Split('.');
            Expression property = parameter;

            // Navigate through the nested properties
            foreach (var field in fields)
            {
                // Retrieve property or field within expression parameter
                property = Expression.PropertyOrField(property, field);
            }
            
            // Retrieve 'Contains' method
            var containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) });
            // Define constant value 
            var valueExpression = Expression.Constant(value, typeof(string));
            // Build up expression
            var containsExpression = Expression.Call(property, containsMethod!, valueExpression);
            
            // Add expression to lambda expression
            return Expression.Lambda<Func<BookEdition, bool>>(containsExpression, parameter);
        }
        catch (Exception)
        {
            return _ => false;
        }
    }

    /// <summary>
    /// Equality filtering 
    /// </summary>
    /// <param name="fieldName"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    private static Expression<Func<BookEdition, bool>> CreateEqualityFilter(
        string fieldName, string? value)
    {
        try
        {
            // Create ParameterExpression node with the specified name and type
            var parameter = Expression.Parameter(typeof(BookEdition));
           
            // Split the field name to navigate nested properties 
            var fields = fieldName.Split('.');
            Expression property = parameter;

            // Navigate through the nested properties
            foreach (var field in fields)
            {
                // Retrieve property or field within expression parameter
                property = Expression.PropertyOrField(property, field);
            }
            
            // Get the type of the property
            var propertyType = property.Type;
            // Handle nullable types by using the underlying type
            var underlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
            
            // Convert the string value to the correct type
            object? convertedValue = null;
            if (!string.IsNullOrEmpty(value))
            {
                convertedValue = Convert.ChangeType(value, underlyingType);
            }
            
            // Define constant value 
            var valueExpression = Expression.Constant(convertedValue, property.Type);
            // Add equality comparision
            var equalsExpression = Expression.Equal(property, valueExpression);

            // Add expression to lambda expression
            return Expression.Lambda<Func<BookEdition, bool>>(equalsExpression, parameter);
        }
        catch (Exception)
        {
            return _ => false;
        }
    }

    /// <summary>
    /// Inequality filtering
    /// </summary>
    /// <param name="fieldName"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    private static Expression<Func<BookEdition, bool>> CreateInEqualityFilter(
        string fieldName, string? value)
    {
        try
        {
            // Create ParameterExpression node with the specified name and type
            var parameter = Expression.Parameter(typeof(BookEdition));
            
            // Split the field name to navigate nested properties 
            var fields = fieldName.Split('.');
            Expression property = parameter;

            // Navigate through the nested properties
            foreach (var field in fields)
            {
                // Retrieve property or field within expression parameter
                property = Expression.PropertyOrField(property, field);
            }
            
            // Get the type of the property
            var propertyType = property.Type;
            // Handle nullable types by using the underlying type
            var underlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
            
            // Convert the string value to the correct type
            object? convertedValue = null;
            if (!string.IsNullOrEmpty(value))
            {
                convertedValue = Convert.ChangeType(value, underlyingType);
            }
            
            // Define constant value 
            var valueExpression = Expression.Constant(convertedValue, property.Type);
            // Add inequality comparision
            var equalsExpression = Expression.NotEqual(property, valueExpression);

            // Add expression to lambda expression
            return Expression.Lambda<Func<BookEdition, bool>>(equalsExpression, parameter);
        }
        catch (Exception)
        {
            return _ => false;
        }
    }

    /// <summary>
    /// Comparision filtering
    /// </summary>
    /// <param name="fieldName"></param>
    /// <param name="value"></param>
    /// <param name="filterOperator"></param>
    /// <returns></returns>
    private static Expression<Func<BookEdition, bool>> CreateComparisonFilter(
        string fieldName, string? value, FilterOperator? filterOperator)
    {
        try
        {
            // Create ParameterExpression node with the specified name and type
            var parameter = Expression.Parameter(typeof(BookEdition));
            
            // Split the field name to navigate nested properties 
            var fields = fieldName.Split('.');
            Expression property = parameter;

            // Navigate through the nested properties
            foreach (var field in fields)
            {
                // Retrieve property or field within expression parameter
                property = Expression.PropertyOrField(property, field);
            }
            
            // Get the type of the property
            var propertyType = property.Type;
            // Handle nullable types by using the underlying type
            var underlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
            
            // Convert the string value to the correct type
            object? convertedValue = null;
            if (!string.IsNullOrEmpty(value))
            {
                convertedValue = Convert.ChangeType(value, underlyingType);
            }

            Expression propertyToCompare = property;
            // Special handling for Datetime: compare Date only 
            if (underlyingType == typeof(DateTime))
            {
                // Extract only date from property value
                propertyToCompare = Expression.Property(property, nameof(DateTime.Date));
                
                // Ensure converted value is also a Date
                if (convertedValue is DateTime dateTimeVal)
                {
                    convertedValue = dateTimeVal.Date;
                }
            }
            
            // Define constant value 
            var valueExpression = Expression.Constant(convertedValue, propertyToCompare.Type);
            
            Expression? comparisonExpression = filterOperator switch
            {
                FilterOperator.GreaterThan => Expression.GreaterThan(propertyToCompare, valueExpression),
                FilterOperator.LessThan => Expression.LessThan(propertyToCompare, valueExpression),
                FilterOperator.GreaterThanOrEqualsTo => Expression.GreaterThanOrEqual(propertyToCompare, valueExpression),
                FilterOperator.LessThanOrEqualsTo => Expression.LessThanOrEqual(propertyToCompare, valueExpression),
                _ => null
            };

            if (comparisonExpression != null)
            {
                // Add expression to lambda expression
                return Expression.Lambda<Func<BookEdition, bool>>(comparisonExpression, parameter);
            }
            
            return _ => false;
        }
        catch (Exception)
        {
            return _ => false;
        }
    }
}

public static class BookEditionSpecificationExtensions
{
    public static List<AdvancedFilter>? FromParamsToListAdvancedFilter(this BookEditionSpecParams specParams)
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