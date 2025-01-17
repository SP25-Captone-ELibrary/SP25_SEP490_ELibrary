using FPTU_ELibrary.Application.Elastic.Models;

namespace FPTU_ELibrary.Application.Elastic.Responses
{
	public record SearchBookResponse(IEnumerable<ElasticLibraryItem> libraryItems,
		int PageIndex, int PageSize, long TotalPage);

	public record SearchBookEditionResponse(
		IEnumerable<ElasticLibraryItem> LibraryItems,
		int PageIndex,
		int PageSize,
		int TotalPage,
		int TotalActualResponse);
}
