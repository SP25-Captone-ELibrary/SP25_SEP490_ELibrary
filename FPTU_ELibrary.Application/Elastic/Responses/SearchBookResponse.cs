using FPTU_ELibrary.Application.Elastic.Models;

namespace FPTU_ELibrary.Application.Elastic.Responses
{
	public record SearchBookResponse(IEnumerable<ElasticBook> Books,
		int PageIndex, int PageSize, long TotalPage);

	public record SearchBookEditionResponse(
		IEnumerable<ElasticBookEdition> BookEditions,
		int PageIndex,
		int PageSize,
		int TotalPage,
		int TotalActualResponse);
}
