using FPTU_ELibrary.Application.Dtos.Borrows;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces;
using FPTU_ELibrary.Domain.Interfaces.Services;
using MapsterMapper;
using Serilog;

namespace FPTU_ELibrary.Application.Services;

public class BorrowDetailExtensionHistoryService : 
    GenericService<BorrowDetailExtensionHistory, BorrowDetailExtensionHistoryDto, int>,
    IBorrowDetailExtensionHistoryService<BorrowDetailExtensionHistoryDto>
{
    public BorrowDetailExtensionHistoryService(
        ISystemMessageService msgService, 
        IUnitOfWork unitOfWork, 
        IMapper mapper, 
        ILogger logger) : base(msgService, unitOfWork, mapper, logger)
    {
    }
}