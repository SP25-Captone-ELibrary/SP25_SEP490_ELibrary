using System.Globalization;
using System.Linq.Expressions;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Specifications.Params;
using Microsoft.EntityFrameworkCore;

namespace FPTU_ELibrary.Domain.Specifications;

public class WarehouseTrackingSpecification : BaseSpecification<WarehouseTracking>
{
    public int PageIndex { get; set; }
    public int PageSize { get; set; }

    public WarehouseTrackingSpecification(WarehouseTrackingSpecParams specParams, int pageIndex, int pageSize)
        : base(w => 
            string.IsNullOrEmpty(specParams.Search) ||
            (
                (!string.IsNullOrEmpty(w.ReceiptNumber) && w.ReceiptNumber.Contains(specParams.Search)) || 
                (!string.IsNullOrEmpty(w.Description) && w.Description.Contains(specParams.Search)) || 
                (!string.IsNullOrEmpty(w.TransferLocation) && w.TransferLocation.Contains(specParams.Search)) 
            )
        )
    {
        // Assign pagination fields
        PageIndex = pageIndex;
        PageSize = pageSize;
        
        // Enable split query
        EnableSplitQuery();
        
        // Apply include
        ApplyInclude(q => q
            .Include(w => w.Supplier)
        );
        
        // Progress filtering 
        // Supplier 
        if (specParams.SupplierId != null)
        {
            AddFilter(w => w.SupplierId == specParams.SupplierId);
        }
        // Tracking type
        if (specParams.TrackingType != null)
        {
            AddFilter(w => w.TrackingType == specParams.TrackingType);
        }
        // Status
        if (specParams.Status != null)
        {
            AddFilter(w => w.Status == specParams.Status);
        }

        // Total item range
        if (specParams.TotalItemRange != null
            && specParams.TotalItemRange.Length > 1)
        {
            if (specParams.TotalItemRange[0].HasValue && specParams.TotalItemRange[1].HasValue)
            {
                AddFilter(x => 
                    x.TotalItem >= specParams.TotalItemRange[0]!.Value
                    && x.TotalItem <= specParams.TotalItemRange[1]!.Value);
            }else if (specParams.TotalItemRange[0] is null && specParams.TotalItemRange[1].HasValue)
            {
                AddFilter(x => x.TotalItem <= specParams.TotalItemRange[1]!.Value);
            }
            else if(specParams.TotalItemRange[0].HasValue && specParams.TotalItemRange[1] is null)
            {
                AddFilter(x => x.TotalItem >= specParams.TotalItemRange[0]!.Value);
            }
        }
        // Total amount range
        if (specParams.TotalAmountRange != null
            && specParams.TotalAmountRange.Length > 1)
        {
            if (specParams.TotalAmountRange[0].HasValue && specParams.TotalAmountRange[1].HasValue)
            {
                AddFilter(x => 
                    x.TotalAmount >= specParams.TotalAmountRange[0]!.Value
                    && x.TotalAmount <= specParams.TotalAmountRange[1]!.Value);
            }else if (specParams.TotalAmountRange[0] is null && specParams.TotalAmountRange[1].HasValue)
            {
                AddFilter(x => x.TotalAmount <= specParams.TotalAmountRange[1]!.Value);
            }
            else if(specParams.TotalAmountRange[0].HasValue && specParams.TotalAmountRange[1] is null)
            {
                AddFilter(x => x.TotalAmount >= specParams.TotalAmountRange[0]!.Value);
            }
        }
        // Entry date range
        if (specParams.EntryDateRange != null
            && specParams.EntryDateRange.Length > 1) 
        {
            if (specParams.EntryDateRange[0].HasValue && specParams.EntryDateRange[1].HasValue)
            {
                AddFilter(x => 
                               x.EntryDate.Date >= specParams.EntryDateRange[0]!.Value.Date
                               && x.EntryDate.Date <= specParams.EntryDateRange[1]!.Value.Date);
            }
            else if (specParams.EntryDateRange[0] is null && specParams.EntryDateRange[1].HasValue)
            {
                AddFilter(x => x.EntryDate.Date <= specParams.EntryDateRange[1]!.Value.Date);
            }
            else if (specParams.EntryDateRange[0].HasValue && specParams.EntryDateRange[1] is null)
            {
                AddFilter(x => x.EntryDate.Date >= specParams.EntryDateRange[0]!.Value.Date);
            }
        }
        // Expected return date range
        if (specParams.ExpectedReturnDateRange != null
            && specParams.ExpectedReturnDateRange.Length > 1) 
        {
            if (specParams.ExpectedReturnDateRange[0].HasValue && specParams.ExpectedReturnDateRange[1].HasValue)
            {
                AddFilter(x => x.ExpectedReturnDate.HasValue && 
                    x.ExpectedReturnDate.Value.Date >= specParams.ExpectedReturnDateRange[0]!.Value.Date
                    && x.ExpectedReturnDate.Value.Date <= specParams.ExpectedReturnDateRange[1]!.Value.Date);
            }
            else if (specParams.ExpectedReturnDateRange[0] is null && specParams.ExpectedReturnDateRange[1].HasValue)
            {
                AddFilter(x => x.ExpectedReturnDate.HasValue && 
                               x.ExpectedReturnDate.Value.Date <= specParams.ExpectedReturnDateRange[1]!.Value.Date);
            }
            else if (specParams.ExpectedReturnDateRange[0].HasValue && specParams.ExpectedReturnDateRange[1] is null)
            {
                AddFilter(x => x.ExpectedReturnDate.HasValue && 
                               x.ExpectedReturnDate.Value.Date >= specParams.ExpectedReturnDateRange[0]!.Value.Date);
            }
        }
        // Actual return date range
        if (specParams.ActualReturnDateRange != null
            && specParams.ActualReturnDateRange.Length > 1) 
        {
            if (specParams.ActualReturnDateRange[0].HasValue && specParams.ActualReturnDateRange[1].HasValue)
            {
                AddFilter(x => x.ActualReturnDate.HasValue && 
                               x.ActualReturnDate.Value.Date >= specParams.ActualReturnDateRange[0]!.Value.Date
                               && x.ActualReturnDate.Value.Date <= specParams.ActualReturnDateRange[1]!.Value.Date);
            }
            else if (specParams.ActualReturnDateRange[0] is null && specParams.ActualReturnDateRange[1].HasValue)
            {
                AddFilter(x => x.ActualReturnDate.HasValue && 
                               x.ActualReturnDate.Value.Date <= specParams.ActualReturnDateRange[1]!.Value.Date);
            }
            else if (specParams.ActualReturnDateRange[0].HasValue && specParams.ActualReturnDateRange[1] is null)
            {
                AddFilter(x => x.ActualReturnDate.HasValue && 
                               x.ActualReturnDate.Value.Date >= specParams.ActualReturnDateRange[0]!.Value.Date);
            }
        }
        // Created at range
        if (specParams.CreatedAtRange != null
            && specParams.CreatedAtRange.Length > 1)
        {
            if (specParams.CreatedAtRange[0].HasValue && specParams.CreatedAtRange[1].HasValue)
            {
                AddFilter(x =>
                    x.CreatedAt >= specParams.CreatedAtRange[0]!.Value.Date
                    && x.CreatedAt <= specParams.CreatedAtRange[1]!.Value.Date);
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
        // Updated at range
        if (specParams.UpdatedAtRange != null
            && specParams.UpdatedAtRange.Length > 1)
        {
            if (specParams.UpdatedAtRange[0].HasValue && specParams.UpdatedAtRange[1].HasValue)
            {
                AddFilter(x => x.UpdatedAt.HasValue &&
                               x.UpdatedAt.Value.Date >= specParams.UpdatedAtRange[0]!.Value.Date
                               && x.UpdatedAt.Value.Date <= specParams.UpdatedAtRange[1]!.Value.Date);
            }
            else if ((specParams.UpdatedAtRange[0] is null && specParams.UpdatedAtRange[1].HasValue))
            {
                AddFilter(x => x.UpdatedAt.HasValue &&
                    x.UpdatedAt.Value.Date <= specParams.UpdatedAtRange[1]!.Value.Date);
            }
            else if (specParams.UpdatedAtRange[0].HasValue && specParams.UpdatedAtRange[1] is null)
            {
                AddFilter(x => x.UpdatedAt.HasValue &&
                    x.UpdatedAt.Value.Date >= specParams.UpdatedAtRange[0]!.Value.Date);
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
            var sortMappings = new Dictionary<string, Expression<Func<WarehouseTracking, object>>>()
            {
                { "RECEIPTNUMBER", x => x.ReceiptNumber },
                { "TRACKINGTYPE", x => x.TrackingType },
                { "TOTALITEM", x => x.TotalItem },
                { "TOTALAMOUNT", x => x.TotalAmount },
                { "SUPPLIERNAME", x => x.Supplier.SupplierName },
                { "SUPPLIERTYPE", x => x.Supplier.SupplierType },
                { "DESCRIPTION", x => x.Description ?? null! },
                { "TRANSFERLOCATION", x => x.TransferLocation ?? null! },
                { "EXPECTEDRETURNDATE", x => x.ExpectedReturnDate ?? null! },
                { "ACTUALRETURNDATE", x => x.ActualReturnDate ?? null! },
                { "CREATEDAT", x => x.CreatedAt },
                { "UPDATEDAT", x => x.UpdatedAt ?? null! },
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
            // Default order by create date
            AddOrderByDescending(u => u.CreatedAt);
        }
    }
}