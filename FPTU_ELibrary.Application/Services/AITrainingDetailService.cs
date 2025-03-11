using FPTU_ELibrary.Application.Dtos.AIServices;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces;
using FPTU_ELibrary.Domain.Interfaces.Services;
using MapsterMapper;
using Serilog;

namespace FPTU_ELibrary.Application.Services;

public class AITrainingDetailService: GenericService<AITrainingDetail, AITrainingDetailDto, int>,
    IAITrainingDetailService<AITrainingDetailDto>
{
    public AITrainingDetailService(
        ISystemMessageService msgService, 
        IUnitOfWork unitOfWork, 
        IMapper mapper, 
        ILogger logger)
        : base(msgService, unitOfWork, mapper, logger)
    {
    }
}
