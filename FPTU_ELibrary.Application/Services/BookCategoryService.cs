using FPTU_ELibrary.Application.Dtos.Books;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces;
using FPTU_ELibrary.Domain.Interfaces.Services;
using MapsterMapper;
using Serilog;

namespace FPTU_ELibrary.Application.Services;

public class BookCategoryService : GenericService<BookCategory, BookCategoryDto, int>, 
    IBookCategoryService<BookCategoryDto>
{
    public BookCategoryService(
        ISystemMessageService msgService, 
        IUnitOfWork unitOfWork, 
        IMapper mapper, 
        ILogger logger) 
    : base(msgService, unitOfWork, mapper, logger)
    {
    }
}