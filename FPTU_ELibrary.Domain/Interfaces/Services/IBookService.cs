using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using FPTU_ELibrary.Domain.Specifications.Interfaces;

namespace FPTU_ELibrary.Domain.Interfaces.Services
{
	public interface IBookService<TDto> : IGenericService<Book, TDto, int>
		where TDto : class
	{
		Task<IServiceResult> GetCreateInformationAsync();
	}
}
