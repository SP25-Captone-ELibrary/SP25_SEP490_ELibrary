using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Dtos.Roles;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using FPTU_ELibrary.Domain.Specifications;
using MapsterMapper;
using Serilog;

using SystemFeatureEnum = FPTU_ELibrary.Domain.Common.Enums.SystemFeature;

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

    public async Task<IServiceResult> GetByNameAsync(SystemFeatureEnum featureName)
    {
        try
        {
            // Get feature by name
            var systemFeatureEntity = await _unitOfWork.Repository<SystemFeature, int>()
                .GetWithSpecAsync(new BaseSpecification<SystemFeature>(
                    sf => sf.EnglishName.Equals(featureName.ToString())));

            if (systemFeatureEntity != null) // Get data success
            {
                return new ServiceResult(ResultCodeConst.SYS_Success0002,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002),
                    _mapper.Map<SystemFeatureDto>(systemFeatureEntity));
            }
            
            // Data not found or empty
            return new ServiceResult(ResultCodeConst.SYS_Warning0004,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004));
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process get feature by name");
        }
    }
}