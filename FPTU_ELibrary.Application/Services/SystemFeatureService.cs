
using FPTU_ELibrary.Application.Dtos.Roles;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces;
using FPTU_ELibrary.Domain.Interfaces.Services;
using MapsterMapper;
using Serilog;

namespace FPTU_ELibrary.Application.Services;

public class SystemFeatureService : GenericService<SystemFeature, SystemFeatureDto, int>,
    ISystemFeatureService<SystemFeatureDto>
{
    public SystemFeatureService(
        ISystemMessageService msgService,
        IUnitOfWork unitOfWork, 
        IMapper mapper,
        ILogger logger) 
        : base(msgService, unitOfWork, mapper, logger)
    {
    }
}