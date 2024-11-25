using FPTU_ELibrary.Application.Elastic.Params;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;

namespace FPTU_ELibrary.Application.Services.IServices
{
	public interface ISearchService
	{
		Task<IServiceResult> SearchBookAsync(SearchBookParameters parameters, 
			CancellationToken cancellationToken);
	}
}
