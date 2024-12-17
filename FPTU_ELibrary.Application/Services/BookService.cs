using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Dtos.Books;
using FPTU_ELibrary.Application.Extensions;
using FPTU_ELibrary.Application.Services.IServices;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using FPTU_ELibrary.Domain.Specifications.Interfaces;
using MapsterMapper;
using Microsoft.Extensions.Caching.Distributed;
using Serilog;

namespace FPTU_ELibrary.Application.Services
{
	public class BookService : GenericService<Book, BookDto, int>, IBookService<BookDto>
	{
		public BookService(
			ISystemMessageService msgService,
			IUnitOfWork unitOfWork,
			IMapper mapper,
			ILogger logger) 
			: base(msgService,unitOfWork, mapper, logger)
		{
		}

		public async Task<IServiceResult> GetAllWithEditionsAndAuthorsAsync(ISpecification<Book> spec)
		{
			var bookEntities = await _unitOfWork.Repository<Book, int>().GetAllWithSpecAsync(spec);
			return new ServiceResult(ResultCodeConst.SYS_Success0002, 
				await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002),
				_mapper.Map<List<BookDto>>(bookEntities));
		}
	}
}
