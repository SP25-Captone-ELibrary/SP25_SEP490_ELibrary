using FPTU_ELibrary.Application.Dtos.Locations;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces;
using FPTU_ELibrary.Domain.Interfaces.Services;
using MapsterMapper;
using Serilog;

namespace FPTU_ELibrary.Application.Services;

public class LibraryShelfService : GenericService<LibraryShelf, LibraryShelfDto, int>,
    ILibraryShelfService<LibraryShelfDto>
{
    public LibraryShelfService(
        ISystemMessageService msgService, 
        IUnitOfWork unitOfWork, 
        IMapper mapper, 
        ILogger logger) : base(msgService, unitOfWork, mapper, logger)
    {
    }
}