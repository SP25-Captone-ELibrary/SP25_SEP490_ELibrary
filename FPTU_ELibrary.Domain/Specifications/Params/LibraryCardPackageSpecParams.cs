using System.Globalization;

namespace FPTU_ELibrary.Domain.Specifications.Params;

public class LibraryCardPackageSpecParams : BaseSpecParams
{
    public decimal? ParsedPrice
    {
        get
        {
            if (decimal.TryParse(Search, out decimal parsedDecimal))
            {
                return parsedDecimal;
            }
            return null;
        }
    }
}