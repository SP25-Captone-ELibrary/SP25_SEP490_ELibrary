using FPTU_ELibrary.Application.Dtos.LibraryItems;
using FPTU_ELibrary.Application.Services.IServices;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces;
using FPTU_ELibrary.Domain.Interfaces.Services;
using MapsterMapper;
using Serilog;

namespace FPTU_ELibrary.Application.Services;

public class LibraryResourceUrlService :GenericService<LibraryResourceUrl, LibraryResourceUrlDto, int>,
    ILibraryResourceUrlService<LibraryResourceUrlDto>
{
    public LibraryResourceUrlService(
        IUnitOfWork unitOfWork,
        ISystemMessageService msgService,
        IMapper mapper,
        ILogger logger) : base(msgService, unitOfWork, mapper, logger)
    {
    }
}