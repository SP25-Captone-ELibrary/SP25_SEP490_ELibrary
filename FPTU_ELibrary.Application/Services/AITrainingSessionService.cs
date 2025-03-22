using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Dtos.AIServices;
using FPTU_ELibrary.Application.Extensions;
using FPTU_ELibrary.Application.Utils;
using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using FPTU_ELibrary.Domain.Specifications;
using MapsterMapper;
using Serilog;

namespace FPTU_ELibrary.Application.Services;

public class AITrainingSessionService : GenericService<AITrainingSession, AITrainingSessionDto, int>,
    IAITrainingSessionService<AITrainingSessionDto>
{
    public AITrainingSessionService(
        ISystemMessageService msgService,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger logger)
        : base(msgService, unitOfWork, mapper, logger)
    {
    }

    public async Task<IServiceResult> UpdateSuccessSessionStatus(int sessionId, bool isSuccess)
    {
        try
        {
            // Determine current system lang
            var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;

            var baseSpec = new BaseSpecification<AITrainingSession>(s 
                => s.TrainingSessionId == sessionId);
            // Check exist warehouse tracking 
            var existingEntity = await _unitOfWork.Repository<AITrainingSession, int>().GetWithSpecAsync(baseSpec);
            if (existingEntity == null)
            {
                var msg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(msg, isEng ? "training session" : "phiên huấn luyện"));
            }
            existingEntity.TrainingStatus = isSuccess ? AITrainingStatus.Completed : AITrainingStatus.Failed;
            // Progress update to DB
            await _unitOfWork.Repository<AITrainingSession, int>().UpdateAsync(existingEntity);
            // Save DB
            var isSaved = await _unitOfWork.SaveChangesAsync() > 0;
            if (isSaved)
            {
                return new ServiceResult(ResultCodeConst.SYS_Success0003,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003), true);
            }

            return new ServiceResult(ResultCodeConst.SYS_Fail0003,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0003), false);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}