using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Dtos.AIServices;
using FPTU_ELibrary.Application.Exceptions;
using FPTU_ELibrary.Application.Extensions;
using FPTU_ELibrary.Application.Services.IServices;
using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using MapsterMapper;
using Serilog;

namespace FPTU_ELibrary.Application.Services;

public class AITrainingImageService : GenericService<AITrainingImage, AITrainingImageDto, int>,
    IAITraningImageService<AITrainingImageDto>
{
    public AITrainingImageService(
        ISystemMessageService msgService, 
        IUnitOfWork unitOfWork, 
        IMapper mapper,
        ILogger logger)
        : base(msgService, unitOfWork, mapper, logger)
    {
    }

    public async Task<IServiceResult> CreateRangeAsync(List<AITrainingImageDto> dtos)
    {
        try
        {
            // Initiate service result
            var serviceResult = new ServiceResult();

            // Determine current lang context
            var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;
            // map to entity
            var listEntity = _mapper.Map<List<AITrainingImage>>(dtos);
            await _unitOfWork.Repository<AITrainingImage, int>().AddRangeAsync(listEntity);

            if (await _unitOfWork.SaveChangesWithTransactionAsync() > 0)
            {
                serviceResult.ResultCode = ResultCodeConst.SYS_Success0001;
                serviceResult.Message = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0001);
                serviceResult.Data = true;
            }
            else
            {
                serviceResult.ResultCode = ResultCodeConst.SYS_Fail0001;
                serviceResult.Message = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0001);
                serviceResult.Data = false;
            }

            return serviceResult;

        }
        catch (UnprocessableEntityException)
        {
            throw;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}