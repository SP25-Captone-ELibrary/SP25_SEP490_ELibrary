using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Dtos.Roles;
using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using FPTU_ELibrary.Domain.Specifications;
using MapsterMapper;
using Serilog;

namespace FPTU_ELibrary.Application.Services;

public class SystemPermissionService : GenericService<SystemPermission, SystemPermissionDto, int>,
    ISystemPermissionService<SystemPermissionDto>
{
    public SystemPermissionService(
        ISystemMessageService msgService, 
        IUnitOfWork unitOfWork, 
        IMapper mapper, 
        ILogger logger) 
        : base(msgService, unitOfWork, mapper, logger)
    {
    }

    public async Task<IServiceResult> GetByPermissionNameAsync(Permission permission)
    {
        try
        {
            // Base spec
            var permissionEntity = await _unitOfWork.Repository<SystemPermission, int>()
                .GetWithSpecAsync(new BaseSpecification<SystemPermission>(
                    sp => sp.EnglishName.Equals(permission.ToString())));

            if (permissionEntity != null)
            {
                return new ServiceResult(ResultCodeConst.SYS_Success0002, 
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002),
                    _mapper.Map<SystemPermissionDto>(permissionEntity));
            }
            
            return new ServiceResult(ResultCodeConst.SYS_Fail0002, 
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0002));
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when progress get permission by name");
        }
    }
}