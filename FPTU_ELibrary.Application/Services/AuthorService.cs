using FPTU_ELibrary.Application.Dtos.Books;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces;
using FPTU_ELibrary.Domain.Interfaces.Services;
using MapsterMapper;
using Serilog;

namespace FPTU_ELibrary.Application.Services;

public class AuthorService : GenericService<Author, AuthorDto, int>, IAuthorService<AuthorDto>
{
    public AuthorService(
        ISystemMessageService msgService, 
        IUnitOfWork unitOfWork, 
        IMapper mapper, 
        ILogger logger) 
        : base(msgService, unitOfWork, mapper, logger)
    {
    }
}