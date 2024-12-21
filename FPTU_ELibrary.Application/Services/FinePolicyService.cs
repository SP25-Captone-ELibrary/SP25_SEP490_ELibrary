using FPTU_ELibrary.Application.Dtos.Fine;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces;
using FPTU_ELibrary.Domain.Interfaces.Services;
using MapsterMapper;
using Serilog;

namespace FPTU_ELibrary.Application.Services.IServices;

public class FinePolicyService :  GenericService<FinePolicy, FinePolicyDto, int>,
    IFinePolicyService<FinePolicyDto>
{
    public FinePolicyService(
        ISystemMessageService msgService
        , IUnitOfWork unitOfWork
        , IMapper mapper
        , ILogger logger
        ) : base(msgService, unitOfWork, mapper, logger)
    {
    }
    
}