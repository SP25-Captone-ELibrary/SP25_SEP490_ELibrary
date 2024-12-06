using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces;
using FPTU_ELibrary.Domain.Interfaces.Services;
using MapsterMapper;
using Serilog;

namespace FPTU_ELibrary.Application.Services;

public class RolePermissionService : GenericService<RolePermission, RolePermissionDto, int>,
    IRolePermissionService<RolePermissionDto>
{
    public RolePermissionService(
        ISystemMessageService msgService,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger logger) 
        : base(msgService, unitOfWork, mapper, logger)
    {
    }
}