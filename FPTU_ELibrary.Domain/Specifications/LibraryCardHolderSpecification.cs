using System.Linq.Expressions;
using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Specifications.Params;
using Microsoft.EntityFrameworkCore;

namespace FPTU_ELibrary.Domain.Specifications;

public class LibraryCardHolderSpecification : BaseSpecification<User>
{
    public int PageIndex { get; set; }
    public int PageSize { get; set; }

    public LibraryCardHolderSpecification(LibraryCardHolderSpecParams specParams, int pageIndex, int pageSize)
        : base(e =>
            // Search with terms
            string.IsNullOrEmpty(specParams.Search) ||
            (
                // Library card
                e.LibraryCard != null && (
                    (!string.IsNullOrEmpty(e.LibraryCard.FullName) && e.LibraryCard.FullName.Contains(specParams.Search)) ||
                    (!string.IsNullOrEmpty(e.LibraryCard.Barcode) && e.LibraryCard.Barcode.Contains(specParams.Search))
                )) ||
                // Email
                (!string.IsNullOrEmpty(e.Email) && e.Email.Contains(specParams.Search)) ||
                // Phone
                (!string.IsNullOrEmpty(e.Phone) && e.Phone.Contains(specParams.Search)) ||
                // Address
                (!string.IsNullOrEmpty(e.Address) && e.Address.Contains(specParams.Search)) ||
                // Individual FirstName and LastName search
                (!string.IsNullOrEmpty(e.FirstName) && e.FirstName.Contains(specParams.Search)) ||
                (!string.IsNullOrEmpty(e.LastName) && e.LastName.Contains(specParams.Search)) 
            )
    {
        // Apply pagination
        PageIndex = pageIndex;
        PageSize = pageSize;
        
        // Apply include
        ApplyInclude(q => q
            .Include(u => u.Role)
            .Include(u => u.LibraryCard!)
        );
        
        // Enable split query
        EnableSplitQuery();
        
        // Exclude all user with role of (Admin)
        AddFilter(u => u.Role.EnglishName != nameof(Role.Administration));
        
        // Add filter
        if (specParams.Gender != null) // Gender
        {
            AddFilter(lch => lch.Gender == nameof(specParams.Gender));
        }
        if (specParams.IssuanceMethod != null) // Issuance method
        {
            AddFilter(lch => lch.LibraryCard != null && lch.LibraryCard.IssuanceMethod == specParams.IssuanceMethod);
        }
        if (specParams.IssuanceMethod != null) // Card status
        {
            AddFilter(lch => lch.LibraryCard != null && lch.LibraryCard.Status == specParams.CardStatus);
        }
        if (specParams.IsAllowBorrowMore != null) // Is allow to borrow more
        {
            AddFilter(lch => lch.LibraryCard != null && lch.LibraryCard.IsAllowBorrowMore == specParams.IsAllowBorrowMore);
        }
        if (specParams.IsReminderSent != null) // Is reminder sent
        {
            AddFilter(lch => lch.LibraryCard != null && lch.LibraryCard.IsReminderSent == specParams.IsReminderSent);
        }
        if (specParams.IsExtended != null) // Whether extend card 
        {
            AddFilter(lch => lch.LibraryCard != null && lch.LibraryCard.IsExtended == specParams.IsExtended);
        }
        if (specParams.IsArchived != null) // Is archived
        {
            AddFilter(lch => lch.LibraryCard != null && lch.LibraryCard.IsArchived == specParams.IsArchived);
        }
        if (specParams.IsDeleted != null) // Is deleted
        {
            AddFilter(lch => lch.IsDeleted == specParams.IsDeleted);
        }
        if (specParams.DobRange != null 
            && specParams.DobRange.Length > 1) // Dob 
        {
            if (specParams.DobRange[0].HasValue && specParams.DobRange[1].HasValue)
            {
                AddFilter(x => x.Dob!.Value.Date >= specParams.DobRange[0]!.Value.Date
                               && x.Dob!.Value.Date <= specParams.DobRange[1]!.Value.Date);
            }
            else if ((specParams.DobRange[0] is null && specParams.DobRange[1].HasValue))
            {
                AddFilter(x => x.Dob!.Value.Date <= specParams.DobRange[1]!.Value.Date);
            }
            else if (specParams.DobRange[0].HasValue && specParams.DobRange[1] is null)
            {
                AddFilter(x => x.Dob!.Value.Date >= specParams.DobRange[0]!.Value.Date);
            }
        }
        if (specParams.CardIssueDateRange != null
            && specParams.CardIssueDateRange.Length > 1) // Card issue date
        {
            if (specParams.CardIssueDateRange[0].HasValue && specParams.CardIssueDateRange[1].HasValue)
            {
                AddFilter(x =>  x.LibraryCard != null &&
                    x.LibraryCard.IssueDate.Date >= specParams.CardIssueDateRange[0]!.Value.Date
                               && x.LibraryCard.IssueDate.Date <= specParams.CardIssueDateRange[1]!.Value.Date);
            }
            else if ((specParams.CardIssueDateRange[0] is null && specParams.CardIssueDateRange[1].HasValue))
            {
                AddFilter(x =>  x.LibraryCard != null &&
                    x.LibraryCard.IssueDate.Date <= specParams.CardIssueDateRange[1]!.Value.Date);
            }
            else if (specParams.CardIssueDateRange[0].HasValue && specParams.CardIssueDateRange[1] is null)
            {
                AddFilter(x =>  x.LibraryCard != null &&
                    x.LibraryCard.IssueDate.Date >= specParams.CardIssueDateRange[0]!.Value.Date);
            }
        }
        if (specParams.CardExpiryDateRange != null
            && specParams.CardExpiryDateRange.Length > 1) // Card expiry date
        {
            if (specParams.CardExpiryDateRange[0].HasValue && specParams.CardExpiryDateRange[1].HasValue)
            {
                AddFilter(x => x.LibraryCard != null && x.LibraryCard.ExpiryDate != null &&
                    x.LibraryCard.ExpiryDate.Value.Date >= specParams.CardExpiryDateRange[0]!.Value.Date
                               && x.LibraryCard.ExpiryDate.Value.Date <= specParams.CardExpiryDateRange[1]!.Value.Date);
            }
            else if ((specParams.CardExpiryDateRange[0] is null && specParams.CardExpiryDateRange[1].HasValue))
            {
                AddFilter(x => x.LibraryCard != null && x.LibraryCard.ExpiryDate != null &&
                    x.LibraryCard.ExpiryDate.Value.Date <= specParams.CardExpiryDateRange[1]!.Value.Date);
            }
            else if (specParams.CardExpiryDateRange[0].HasValue && specParams.CardExpiryDateRange[1] is null)
            {
                AddFilter(x => x.LibraryCard != null && x.LibraryCard.ExpiryDate != null &&
                    x.LibraryCard.ExpiryDate.Value.Date >= specParams.CardExpiryDateRange[0]!.Value.Date);
            }
        }
        if (specParams.SuspensionDateRange != null
            && specParams.SuspensionDateRange.Length > 1) // Card suspension end date
        {
            if (specParams.SuspensionDateRange[0].HasValue && specParams.SuspensionDateRange[1].HasValue)
            {
                AddFilter(x => x.LibraryCard != null && x.LibraryCard.SuspensionEndDate != null &&
                               x.LibraryCard.SuspensionEndDate.Value.Date >= specParams.SuspensionDateRange[0]!.Value.Date
                               && x.LibraryCard.SuspensionEndDate.Value.Date <= specParams.SuspensionDateRange[1]!.Value.Date);
            }
            else if ((specParams.SuspensionDateRange[0] is null && specParams.SuspensionDateRange[1].HasValue))
            {
                AddFilter(x => x.LibraryCard != null && x.LibraryCard.SuspensionEndDate != null &&
                               x.LibraryCard.SuspensionEndDate.Value.Date <= specParams.SuspensionDateRange[1]!.Value.Date);
            }
            else if (specParams.SuspensionDateRange[0].HasValue && specParams.SuspensionDateRange[1] is null)
            {
                AddFilter(x => x.LibraryCard != null && x.LibraryCard.SuspensionEndDate != null &&
                               x.LibraryCard.SuspensionEndDate.Value.Date >= specParams.SuspensionDateRange[0]!.Value.Date);
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
            var sortMappings = new Dictionary<string, Expression<Func<User, object>>>()
            {
                { "EMAIL", x => x.Email },
                { "PHONE", x => x.Phone ?? string.Empty },
                { "ADDRESS", x => x.Phone ?? string.Empty },
                { "GENDER", x => x.Gender ?? string.Empty },
                { "DOB", x => x.Dob ?? null! },
                { "ISACTIVE", x => x.IsActive},
                { "ISDELETED", x => x.IsDeleted},
                { "CREATEDATE", x => x.CreateDate},
                { "MODIFIEDDATE", x => x.ModifiedDate ?? null!},
                { "ISDELETED", x => x.IsDeleted},
                { "FULLNAME", x => x.LibraryCard != null ? x.LibraryCard.FullName : null! },
                { "BARCODE", x => x.LibraryCard != null ? x.LibraryCard.Barcode : null! },
                { "ISSUANCEMETHOD", x => x.LibraryCard != null ? x.LibraryCard.IssuanceMethod : null! },
                { "STATUS", x => x.LibraryCard != null ? x.LibraryCard.Status : null! },
                { "ISALLOWBORROWMORE", x => x.LibraryCard != null ? x.LibraryCard.IsAllowBorrowMore : null! },
                { "MAXITEMONCETIME", x => x.LibraryCard != null ? x.LibraryCard.MaxItemOnceTime : null! },
                { "ALLOWBORROWMOREREASON", x => x.LibraryCard != null ? x.LibraryCard.AllowBorrowMoreReason : null! },
                { "ISREMINDERSENT", x => x.LibraryCard != null ? x.LibraryCard.IsReminderSent : null! },
                { "TOTALMISSEDPICKUP", x => x.LibraryCard != null ? x.LibraryCard.TotalMissedPickUp : null! },
                { "ISEXTENDED", x => x.LibraryCard != null ? x.LibraryCard.IsExtended : null! },
                { "EXTENSIONCOUNT", x => x.LibraryCard != null ? x.LibraryCard.ExtensionCount : null! },
                { "ISSUEDATE", x => x.LibraryCard != null ? x.LibraryCard.IssueDate : null! },
                { "EXPIRYDATE", x => x.LibraryCard != null ? x.LibraryCard.ExpiryDate : null! },
                { "SUSPENSIONENDDATE", x => x.LibraryCard != null ? x.LibraryCard.SuspensionEndDate : null! },
                { "SUSPENSIONREASON", x => x.LibraryCard != null ? x.LibraryCard.SuspensionReason : null! },
                { "ISARCHIVED", x => x.LibraryCard != null ? x.LibraryCard.IsArchived : null! },
                { "ARCHIVEREASON", x => x.LibraryCard != null ? x.LibraryCard.ArchiveReason : null! },
            };
        
            // Get sorting pattern
            if (sortMappings.TryGetValue(specParams.Sort.ToUpper(), 
                    out var sortExpression))
            {
                if(isDescending) AddOrderByDescending(sortExpression);
                else AddOrderBy(sortExpression);    
            }
        }
        else
        {
            // Default sort by create date 
            AddOrderByDescending(u => u.CreateDate);
        }
    }
}