using FPTU_ELibrary.Application.Elastic.Models;

namespace FPTU_ELibrary.Application.Elastic.Responses
{
	public record SearchBookResponse(IEnumerable<ElasticBook> Books,
		int PageIndex, int PageSize, long TotalPage);
}
