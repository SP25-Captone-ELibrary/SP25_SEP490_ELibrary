using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.Application.Elastic.Params
{
	public record SearchItemParameters(
		#region Quick search
		string? SearchText,
		SearchKeyword? SearchWithKeyword,
		#endregion 
		
		#region Basic search
		string? Title,
		string? Author,
		string? Isbn,
		string? ClassificationNumber,
		string? Genres,
		string? Publisher,
		string? TopicalTerms,
		#endregion
		
		#region Advanced search
		string[]? F,
		FilterOperator[]? O,
		string[]? V,
		#endregion
		
		// Fields to determine type of search
		SearchType SearchType,
		bool SearchWithSpecial,
		bool IsMatchExact,
		int PageIndex, 
		int PageSize
	);
}
