using FPTU_ELibrary.Application.Dtos.LibraryItems;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using MapsterMapper;
using MimeKit.Encodings;
using Serilog;

namespace FPTU_ELibrary.Application.Services;

public class LibraryItemGroupService : GenericService<LibraryItemGroup, LibraryItemGroupDto, int>,
    ILibraryItemGroupService<LibraryItemGroupDto>
{
    public LibraryItemGroupService(
        ISystemMessageService msgService, 
        IUnitOfWork unitOfWork, 
        IMapper mapper, 
        ILogger logger) 
    : base(msgService, unitOfWork, mapper, logger)
    {
    }
}