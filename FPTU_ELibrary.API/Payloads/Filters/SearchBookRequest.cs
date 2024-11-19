using FPTU_ELibrary.Application.Elastic.Params;
using Nest;

namespace FPTU_ELibrary.API.Payloads.Filters
{
    public class SearchBookRequest
    {
        private string? _sort;
        private bool? _isDescendingSort;

        public string? Sort
        {
            get => _sort;
            set
            {
                _sort = value;
                if (!string.IsNullOrEmpty(_sort) && !_isDescendingSort.HasValue)
                {
                    // Default to ascending if IsDescendingSort is not set
                    _isDescendingSort = false;
                }
            }
        }
        public bool? IsDecendingSort
        {
            get => _isDescendingSort;
            set => _isDescendingSort = value;
        }

        // Base params fields
        public string? SearchText { get; set; }
        public int PageIndex { get; set; }
        public int PageSize { get; set; }

        // Book filtering fields
        public int? PublicationYear { get; set; }
        public string? Languages { get; set; }
        public int? MaxPageCount { get; set; }
        public int? IsDeleted { get; set; }
        public int? IsDraft { get; set; }
    }

    public static class SearchBookRequestExtensions
    {
		public static SearchBookParameters ToSearchBookParams(this SearchBookRequest req)
			=> new(
				// Base search fields
				req.SearchText,
				// Sorting fields
				req.Sort,
				req.IsDecendingSort,
				// Filtering
				req.PublicationYear,
				req.Languages,
				req.MaxPageCount,
				req.IsDeleted,
				req.IsDraft,
				// Offset
				(req.PageIndex - 1) * req.PageSize,
				// Total take elements
				req.PageSize
			);
	}
}
