using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Services.Base;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Specifications.Interfaces;

namespace FPTU_ELibrary.Domain.Interfaces.Services
{
	public interface IBookService : IGenericService<Book, BookDto, int>
	{
		Task<IServiceResult> GetAllWithEditionsAndAuthorsAsync(ISpecification<Book> spec);
	}
}
