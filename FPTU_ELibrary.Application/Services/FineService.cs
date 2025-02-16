using FPTU_ELibrary.Application.Dtos.Fine;
using FPTU_ELibrary.Application.Services.IServices;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces;
using FPTU_ELibrary.Domain.Interfaces.Services;
using MapsterMapper;
using Serilog;

namespace FPTU_ELibrary.Application.Services;

public class FineService : GenericService<Fine, FineDto, int>, IFineService<FineDto>
{
    public FineService(ISystemMessageService msgService,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger logger) : base(msgService, unitOfWork, mapper, logger)
    {
    }
}