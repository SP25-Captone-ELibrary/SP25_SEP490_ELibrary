using System.Linq.Expressions;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Specifications.Params;

namespace FPTU_ELibrary.Domain.Specifications;

public class ReservationQueueSpecification : BaseSpecification<ReservationQueue>
{
    public int PageIndex { get; set; }
    public int PageSize { get; set; }
    
    public ReservationQueueSpecification(ReservationQueueSpecParams specParams, int pageIndex, int pageSize,
        string? email = null, Guid? userId = null) 
    : base(r => 
        string.IsNullOrEmpty(specParams.Search) || 
        (
            // LibraryItem
            (!string.IsNullOrEmpty(r.LibraryItem.Title) && r.LibraryItem.Title.Contains(specParams.Search)) ||
            (!string.IsNullOrEmpty(r.LibraryItem.SubTitle) && r.LibraryItem.SubTitle.Contains(specParams.Search)) ||
            (!string.IsNullOrEmpty(r.LibraryItem.Responsibility) && r.LibraryItem.Responsibility.Contains(specParams.Search)) ||
            (!string.IsNullOrEmpty(r.LibraryItem.Edition) && r.LibraryItem.Edition.Contains(specParams.Search)) ||
            (!string.IsNullOrEmpty(r.LibraryItem.Language) && r.LibraryItem.Language.Contains(specParams.Search)) ||
            (!string.IsNullOrEmpty(r.LibraryItem.OriginLanguage) && r.LibraryItem.OriginLanguage.Contains(specParams.Search)) ||
            (!string.IsNullOrEmpty(r.LibraryItem.Summary) && r.LibraryItem.Summary.Contains(specParams.Search)) ||
            (!string.IsNullOrEmpty(r.LibraryItem.Publisher) && r.LibraryItem.Publisher.Contains(specParams.Search)) ||
            (!string.IsNullOrEmpty(r.LibraryItem.PublicationPlace) && r.LibraryItem.PublicationPlace.Contains(specParams.Search)) ||
            (!string.IsNullOrEmpty(r.LibraryItem.ClassificationNumber) && r.LibraryItem.ClassificationNumber.Contains(specParams.Search)) ||
            (!string.IsNullOrEmpty(r.LibraryItem.CutterNumber) && r.LibraryItem.CutterNumber.Contains(specParams.Search)) ||
            (!string.IsNullOrEmpty(r.LibraryItem.Isbn) && r.LibraryItem.Isbn.Contains(specParams.Search)) ||
            (!string.IsNullOrEmpty(r.LibraryItem.Ean) && r.LibraryItem.Ean.Contains(specParams.Search)) ||
            (!string.IsNullOrEmpty(r.LibraryItem.PhysicalDetails) && r.LibraryItem.PhysicalDetails.Contains(specParams.Search)) ||
            (!string.IsNullOrEmpty(r.LibraryItem.Dimensions) && r.LibraryItem.Dimensions.Contains(specParams.Search)) ||
            (!string.IsNullOrEmpty(r.LibraryItem.AccompanyingMaterial) && r.LibraryItem.AccompanyingMaterial.Contains(specParams.Search)) ||
            (!string.IsNullOrEmpty(r.LibraryItem.Genres) && r.LibraryItem.Genres.Contains(specParams.Search)) ||
            (!string.IsNullOrEmpty(r.LibraryItem.GeneralNote) && r.LibraryItem.GeneralNote.Contains(specParams.Search)) ||
            (!string.IsNullOrEmpty(r.LibraryItem.BibliographicalNote) && r.LibraryItem.BibliographicalNote.Contains(specParams.Search)) ||
            (!string.IsNullOrEmpty(r.LibraryItem.TopicalTerms) && r.LibraryItem.TopicalTerms.Contains(specParams.Search)) ||
            (!string.IsNullOrEmpty(r.LibraryItem.AdditionalAuthors) && r.LibraryItem.AdditionalAuthors.Contains(specParams.Search)) ||
            // Category
            (!string.IsNullOrEmpty(r.LibraryItem.Category.EnglishName) &&
             r.LibraryItem.Category.EnglishName.Contains(specParams.Search)) ||
            (!string.IsNullOrEmpty(r.LibraryItem.Category.VietnameseName) &&
             r.LibraryItem.Category.VietnameseName.Contains(specParams.Search)) ||
            // LibraryItemAuthors
            // LibraryItem -> LibraryItemAuthors
            r.LibraryItem.LibraryItemAuthors.Any(a =>
                !string.IsNullOrEmpty(a.Author.AuthorCode) && a.Author.AuthorCode.Contains(specParams.Search) ||
                !string.IsNullOrEmpty(a.Author.FullName) && a.Author.FullName.Contains(specParams.Search) ||
                !string.IsNullOrEmpty(a.Author.Biography) && a.Author.Biography.Contains(specParams.Search) ||
                !string.IsNullOrEmpty(a.Author.Nationality) && a.Author.Nationality.Contains(specParams.Search)
            ) ||
            // LibraryItemInstances
            // LibraryItem -> LibraryItemInstances
            r.LibraryItem.LibraryItemInstances.Any(bec =>
                !string.IsNullOrEmpty(bec.Barcode) && bec.Barcode.Contains(specParams.Search) 
            )
        )
    )
    {
        // Pagination
        PageIndex = pageIndex;
        PageSize = pageSize;
        
        // Enable split query
        EnableSplitQuery();
        
        // Add filter
        if (!string.IsNullOrEmpty(email)) // For specific email (privacy)
        {
            AddFilter(r => r.LibraryCard.Users.Any(u => u.Email == email));
        }
        if (userId != null && userId != Guid.Empty) // For specific user (management)
        {
            AddFilter(r => r.LibraryCard.Users.Any(u => u.UserId == userId));
        }
        if (specParams.QueueStatus != null) // Reservation queue status
        {
            AddFilter(r => r.QueueStatus == specParams.QueueStatus);
        }
        if (specParams.IsReservedAfterRequestFailed != null) // Is reserved after request failed
        {
            AddFilter(r => r.IsReservedAfterRequestFailed == specParams.IsReservedAfterRequestFailed);
        }
        if (specParams.IsAppliedLabel != null) // Is applied label
        {
            AddFilter(r => r.IsAppliedLabel == specParams.IsAppliedLabel);
        }
        if (specParams.IsNotified != null) // Is notified
        {
            AddFilter(r => r.IsNotified == specParams.IsNotified);
        }
        
        if (specParams.ReservationDateRange != null
            && specParams.ReservationDateRange.Length > 1) // With range of reservation date
        {
            if (specParams.ReservationDateRange[0].HasValue && specParams.ReservationDateRange[1].HasValue)
            {
                AddFilter(x =>
                    x.ReservationDate.Date >= specParams.ReservationDateRange[0]!.Value.Date
                    && x.ReservationDate.Date <= specParams.ReservationDateRange[1]!.Value.Date);
            }
            else if (specParams.ReservationDateRange[0] is null && specParams.ReservationDateRange[1].HasValue)
            {
                AddFilter(x => x.ReservationDate.Date <= specParams.ReservationDateRange[1]);
            }
            else if (specParams.ReservationDateRange[0].HasValue && specParams.ReservationDateRange[1] is null)
            {
                AddFilter(x => x.ReservationDate.Date >= specParams.ReservationDateRange[0]);
            }
        }
        
        if (specParams.ExpiryDateRange != null
            && specParams.ExpiryDateRange.Length > 1) // With range of expiry date
        {
            if (specParams.ExpiryDateRange[0].HasValue && specParams.ExpiryDateRange[1].HasValue)
            {
                AddFilter(x => x.ExpiryDate.HasValue &&
                               x.ExpiryDate.Value.Date >= specParams.ExpiryDateRange[0]!.Value.Date
                               && x.ExpiryDate.Value.Date <= specParams.ExpiryDateRange[1]!.Value.Date);
            }
            else if ((specParams.ExpiryDateRange[0] is null && specParams.ExpiryDateRange[1].HasValue))
            {
                AddFilter(x => x.ExpiryDate != null && 
                               x.ExpiryDate.Value.Date <= specParams.ExpiryDateRange[1]!.Value.Date);
            }
            else if (specParams.ExpiryDateRange[0].HasValue && specParams.ExpiryDateRange[1] is null)
            {
                AddFilter(x => x.ExpiryDate != null && 
                               x.ExpiryDate.Value.Date >= specParams.ExpiryDateRange[0]!.Value.Date);
            }
        }
        
        if (specParams.AssignDateRange != null
            && specParams.AssignDateRange.Length > 1) // With range of assign date
        {
            if (specParams.AssignDateRange[0].HasValue && specParams.AssignDateRange[1].HasValue)
            {
                AddFilter(x => x.AssignedDate.HasValue &&
                               x.AssignedDate.Value.Date >= specParams.AssignDateRange[0]!.Value.Date
                               && x.AssignedDate.Value.Date <= specParams.AssignDateRange[1]!.Value.Date);
            }
            else if ((specParams.AssignDateRange[0] is null && specParams.AssignDateRange[1].HasValue))
            {
                AddFilter(x => x.AssignedDate != null && 
                               x.AssignedDate.Value.Date <= specParams.AssignDateRange[1]!.Value.Date);
            }
            else if (specParams.AssignDateRange[0].HasValue && specParams.AssignDateRange[1] is null)
            {
                AddFilter(x => x.AssignedDate != null && 
                               x.AssignedDate.Value.Date >= specParams.AssignDateRange[0]!.Value.Date);
            }
        }
        
        if (specParams.CollectedDateRange != null
            && specParams.CollectedDateRange.Length > 1) // With range of collected date
        {
            if (specParams.CollectedDateRange[0].HasValue && specParams.CollectedDateRange[1].HasValue)
            {
                AddFilter(x => x.CollectedDate.HasValue &&
                               x.CollectedDate.Value.Date >= specParams.CollectedDateRange[0]!.Value.Date
                               && x.CollectedDate.Value.Date <= specParams.CollectedDateRange[1]!.Value.Date);
            }
            else if ((specParams.CollectedDateRange[0] is null && specParams.CollectedDateRange[1].HasValue))
            {
                AddFilter(x => x.CollectedDate != null && 
                               x.CollectedDate.Value.Date <= specParams.CollectedDateRange[1]!.Value.Date);
            }
            else if (specParams.CollectedDateRange[0].HasValue && specParams.CollectedDateRange[1] is null)
            {
                AddFilter(x => x.CollectedDate != null && 
                               x.CollectedDate.Value.Date >= specParams.CollectedDateRange[0]!.Value.Date);
            }
        }
        
        if (specParams.ExpectedAvailableDateMinRange != null
            && specParams.ExpectedAvailableDateMinRange.Length > 1) // With range of expected available min date
        {
            if (specParams.ExpectedAvailableDateMinRange[0].HasValue && specParams.ExpectedAvailableDateMinRange[1].HasValue)
            {
                AddFilter(x => x.ExpectedAvailableDateMin.HasValue &&
                               x.ExpectedAvailableDateMin.Value.Date >= specParams.ExpectedAvailableDateMinRange[0]!.Value.Date
                               && x.ExpectedAvailableDateMin.Value.Date <= specParams.ExpectedAvailableDateMinRange[1]!.Value.Date);
            }
            else if ((specParams.ExpectedAvailableDateMinRange[0] is null && specParams.ExpectedAvailableDateMinRange[1].HasValue))
            {
                AddFilter(x => x.ExpectedAvailableDateMin != null && 
                               x.ExpectedAvailableDateMin.Value.Date <= specParams.ExpectedAvailableDateMinRange[1]!.Value.Date);
            }
            else if (specParams.ExpectedAvailableDateMinRange[0].HasValue && specParams.ExpectedAvailableDateMinRange[1] is null)
            {
                AddFilter(x => x.ExpectedAvailableDateMin != null && 
                               x.ExpectedAvailableDateMin.Value.Date >= specParams.ExpectedAvailableDateMinRange[0]!.Value.Date);
            }
        }
        
        if (specParams.ExpectedAvailableDateMaxRange != null
            && specParams.ExpectedAvailableDateMaxRange.Length > 1) // With range of expected available max date
        {
            if (specParams.ExpectedAvailableDateMaxRange[0].HasValue && specParams.ExpectedAvailableDateMaxRange[1].HasValue)
            {
                AddFilter(x => x.ExpectedAvailableDateMax.HasValue &&
                               x.ExpectedAvailableDateMax.Value.Date >= specParams.ExpectedAvailableDateMaxRange[0]!.Value.Date
                               && x.ExpectedAvailableDateMax.Value.Date <= specParams.ExpectedAvailableDateMaxRange[1]!.Value.Date);
            }
            else if ((specParams.ExpectedAvailableDateMaxRange[0] is null && specParams.ExpectedAvailableDateMaxRange[1].HasValue))
            {
                AddFilter(x => x.ExpectedAvailableDateMax != null && 
                               x.ExpectedAvailableDateMax.Value.Date <= specParams.ExpectedAvailableDateMaxRange[1]!.Value.Date);
            }
            else if (specParams.ExpectedAvailableDateMaxRange[0].HasValue && specParams.ExpectedAvailableDateMaxRange[1] is null)
            {
                AddFilter(x => x.ExpectedAvailableDateMax != null && 
                               x.ExpectedAvailableDateMax.Value.Date >= specParams.ExpectedAvailableDateMaxRange[0]!.Value.Date);
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
            // Default order by reservation date
            AddOrderByDescending(u => u.ReservationDate);
        }
    }
    
    private void ApplySorting(string propertyName, bool isDescending)
    {
        if (string.IsNullOrEmpty(propertyName)) return;

        // Initialize expression parameter with type of Transaction (x)
        var parameter = Expression.Parameter(typeof(ReservationQueue), "x");
        // Assign property base on property name (x.PropertyName)
        var property = Expression.Property(parameter, propertyName);
        // Building a complete sort lambda expression (x => x.PropertyName)
        var sortExpression =
            Expression.Lambda<Func<ReservationQueue, object>>(Expression.Convert(property, typeof(object)), parameter);

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