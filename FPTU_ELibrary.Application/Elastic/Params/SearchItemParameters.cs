using FPTU_ELibrary.Domain.Common.Enums;
using Nest;
namespace FPTU_ELibrary.Application.Elastic.Params
{
	public record SearchItemParameters(
		string? SearchText,
		SearchKeyword? SearchWithKeyword,
		bool SearchWithSpecial,
		bool IsMatchExact,
		bool? IsDeleted, 
		bool? CanBorrow,
		string[]? F,
		FilterOperator[]? O,
		string[]? V,
		int PageIndex, 
		int PageSize
	);
}
