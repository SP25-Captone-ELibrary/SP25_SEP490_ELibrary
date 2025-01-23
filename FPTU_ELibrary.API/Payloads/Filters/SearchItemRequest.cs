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
                SearchText: req.Search,
                SearchWithKeyword: req.SearchWithKeyword,
                SearchWithSpecial: req.SearchWithSpecial,
                IsMatchExact: req.IsMatchExact,
                Title: req.Title,
                Author: req.Author,
                Isbn: req.Isbn,
                ClassificationNumber: req.ClassificationNumber,
                Genres: req.Genres,
                Publisher: req.Publisher,
                TopicalTerms: req.TopicalTerms,
                F: req.F,
                O: req.O,
                V: req.V,
                SearchType: req.SearchType,
                PageIndex: req.PageIndex ?? 0,
                PageSize: req.PageSize ?? 0
            );
        }
	}
}
