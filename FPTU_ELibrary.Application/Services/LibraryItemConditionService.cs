using FPTU_ELibrary.Application.Dtos.LibraryItems;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces;
using FPTU_ELibrary.Domain.Interfaces.Services;
using MapsterMapper;
using Serilog;

namespace FPTU_ELibrary.Application.Services;

public class LibraryItemConditionService : GenericService<LibraryItemCondition, LibraryItemConditionDto, int>,
    ILibraryItemConditionService<LibraryItemConditionDto>
{
    public LibraryItemConditionService(
        ISystemMessageService msgService, 
        IUnitOfWork unitOfWork, 
        IMapper mapper, 
        ILogger logger) : base(msgService, unitOfWork, mapper, logger)
    {
    }
}