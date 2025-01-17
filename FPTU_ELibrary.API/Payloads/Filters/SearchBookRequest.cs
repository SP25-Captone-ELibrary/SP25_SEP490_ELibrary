using FPTU_ELibrary.Application.Elastic.Params;
using FPTU_ELibrary.Domain.Specifications.Params;
using Nest;

namespace FPTU_ELibrary.API.Payloads.Filters
{
    public class SearchBookRequest : LibraryItemSpecParams
    {
        public bool? IsDescendingSort { get; set; }
    }

    public static class SearchBookRequestExtensions
    {
        public static SearchBookParameters ToSearchBookParams(this SearchBookRequest req)
        {
            return new(
                // Base search fields
                SearchText: req.Search,
                // Sorting fields
                Sort: req.Sort,
                IsDescendingSort: req.IsDescendingSort,
                // Filtering fields
                IsDeleted: req.IsDeleted,
                CanBorrow: req.CanBorrow,
                F: req.F,
                O: req.O,
                V: req.V,
                PageIndex: req.PageIndex ?? 0,
                PageSize: req.PageSize ?? 0
            );
        }
	}
}
