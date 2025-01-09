using FPTU_ELibrary.Domain.Common.Enums;
using Nest;
namespace FPTU_ELibrary.Application.Elastic.Params
{
	public record SearchBookParameters(
		string? SearchText,
		string? Sort, 
		bool? IsDescendingSort, 
		bool? IsDeleted, 
		bool? CanBorrow,
		string[]? F,
		FilterOperator[]? O,
		string[]? V,
		int PageIndex, 
		int PageSize
	);
}
