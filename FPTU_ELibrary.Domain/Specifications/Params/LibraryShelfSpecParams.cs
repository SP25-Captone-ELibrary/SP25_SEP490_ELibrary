using System.Globalization;

namespace FPTU_ELibrary.Domain.Specifications.Params;

public class LibraryShelfSpecParams : BaseSpecParams
{
    public bool? IsChildrenSection { get; set; }
    public bool? IsReferenceSection { get; set; }
    public bool? IsJournalSection { get; set; }
    
    public decimal? ParsedSearchDecimal
    {
        get
        {
            if (decimal.TryParse(Search, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal parsedDecimal))
            {
                // Rounds a decimal value to 2 
                decimal roundedValue = Math.Round(parsedDecimal, 2);
            
                // Add decimal(10,2) limitation
                const decimal maxAllowed = 99999999.99m;
                const decimal minAllowed = -99999999.99m;
            
                // Check if any exceed or smaller than limit values
                if (roundedValue > maxAllowed || roundedValue < minAllowed)
                {
                    return 0;
                }
            
                return roundedValue;
            }
            return null;
        }
    }
}