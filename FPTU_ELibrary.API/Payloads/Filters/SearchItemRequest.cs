using FPTU_ELibrary.Application.Elastic.Params;
using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Specifications.Params;
using Nest;

namespace FPTU_ELibrary.API.Payloads.Filters
{
    public class SearchItemRequest : LibraryItemSpecParams
    {
        public bool IsMatchExact { get; set; } = false;
        public bool SearchWithSpecial { get; set; } = true;
        public SearchKeyword? SearchWithKeyword { get; set; }
    }

    public static class SearchItemRequestExtensions
    {
        public static SearchItemParameters ToSearchItemParams(this SearchItemRequest req)
        {
            return new(
                // Base search fields
                SearchText: req.Search,
                // Search with specific keyword (title, author, isbn, etc.)
                SearchWithKeyword: req.SearchWithKeyword,
                // Filtering fields
                SearchWithSpecial: req.SearchWithSpecial,
                IsDeleted: req.IsDeleted,
                CanBorrow: req.CanBorrow,
                IsMatchExact: req.IsMatchExact,
                F: req.F,
                O: req.O,
                V: req.V,
                PageIndex: req.PageIndex ?? 0,
                PageSize: req.PageSize ?? 0
            );
        }
	}
}
