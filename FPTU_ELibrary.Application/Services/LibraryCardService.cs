using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Dtos.LibraryCard;
using FPTU_ELibrary.Application.Extensions;
using FPTU_ELibrary.Application.Utils;
using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using MapsterMapper;
using Serilog;

namespace FPTU_ELibrary.Application.Services;

public class LibraryCardService : GenericService<LibraryCard, LibraryCardDto, Guid>,
    ILibraryCardService<LibraryCardDto>
{
    public LibraryCardService(
        ISystemMessageService msgService, 
        IUnitOfWork unitOfWork, 
        IMapper mapper, 
        ILogger logger) : base(msgService, unitOfWork, mapper, logger)
    {
    }

    public async Task<IServiceResult> CheckCardValidityAsync(Guid libraryCardId)
    {
        try
        {
            // Determine current lang context
            var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;
            
            // Check existing card
            var existingEntity = await _unitOfWork.Repository<LibraryCard, Guid>().GetByIdAsync(libraryCardId);
            if (existingEntity == null)
            {
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, isEng ? "library card" : "thẻ thư viện"));
            }
            
            // Current local datetime
            var currentLocalDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
            	// Vietnam timezone
            	TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
            
            // Card status 
            switch (existingEntity.Status)
            {
                case LibraryCardStatus.Active:
                    // Continue to check for other information
                    break;
                case LibraryCardStatus.Pending:
                    // Your library card is not activated yet. Please make a payment to activate your card
                    return new ServiceResult(ResultCodeConst.LibraryCard_Warning0001,
                        await _msgService.GetMessageAsync(ResultCodeConst.LibraryCard_Warning0001));
                case LibraryCardStatus.Expired:
                    // Check for expiry date
                    if (existingEntity.ExpiryDate != null &&
                        existingEntity.ExpiryDate < currentLocalDateTime)
                    {
                        // Your library card has expired. Please renew it to continue using library services
                        return new ServiceResult(ResultCodeConst.LibraryCard_Warning0002,
                            await _msgService.GetMessageAsync(ResultCodeConst.LibraryCard_Warning0002));
                    }
                    break;
                case LibraryCardStatus.Suspended:
                    // Check for suspension end date
                    if (existingEntity.SuspensionEndDate != null &&
                        existingEntity.SuspensionEndDate > currentLocalDateTime)
                    {
                        var msg = await _msgService.GetMessageAsync(ResultCodeConst.LibraryCard_Warning0003);
                        // Your library card has been suspended due to a violation or administrative action. Please contact the library for assistance
                        return new ServiceResult(ResultCodeConst.LibraryCard_Warning0003,
                            StringUtils.Format(msg, existingEntity.SuspensionEndDate.Value.ToString("MM/dd/yyyy")));
                    }
                    break;
            }
            
            // Valid card
            return new ServiceResult(ResultCodeConst.LibraryCard_Success0001,
                await _msgService.GetMessageAsync(ResultCodeConst.LibraryCard_Success0001));
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process check library card validity");
        }
    }

    public async Task<IServiceResult> UpdateBorrowMoreStatusWithoutSaveChangesAsync(Guid libraryCardId)
    {
        try
        {
            // Check exist entity
            var existingEntity = await _unitOfWork.Repository<LibraryCard, Guid>().GetByIdAsync(libraryCardId);
            if (existingEntity == null)
            {
                // Fail to update
                return new ServiceResult(ResultCodeConst.SYS_Fail0003,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0003));
            }

            // Process update status
            existingEntity.IsAllowBorrowMore = false;
            existingEntity.MaxItemOnceTime = 0;
            
            // Perform update
            await _unitOfWork.Repository<LibraryCard, Guid>().UpdateAsync(existingEntity);
            
            // Update success
            return new ServiceResult(ResultCodeConst.SYS_Fail0003,
                                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0003));
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process update borrow more status without saving");
        }
    }
}