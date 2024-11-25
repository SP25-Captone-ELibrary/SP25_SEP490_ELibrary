using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using FPTU_ELibrary.Domain.Specifications.Interfaces;
using MapsterMapper;

namespace FPTU_ELibrary.Application.Services
{
	public class BookService : GenericService<Book, BookDto, int>, IBookService<BookDto>
	{
		public BookService(IUnitOfWork unitOfWork, IMapper mapper) 
			: base(unitOfWork, mapper)
		{
		}

		public async Task<IServiceResult> GetAllWithEditionsAndAuthorsAsync(ISpecification<Book> spec)
		{
			var bookEntities = await _unitOfWork.Repository<Book, int>().GetAllWithSpecAsync(spec);
			return new ServiceResult(ResultConst.SUCCESS_READ_CODE, ResultConst.SUCCESS_READ_MSG, 
				_mapper.Map<List<BookDto>>(bookEntities));
		}
	}
}
