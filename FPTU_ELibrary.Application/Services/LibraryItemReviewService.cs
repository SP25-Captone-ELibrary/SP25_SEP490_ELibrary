using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Dtos.LibraryItems;
using FPTU_ELibrary.Application.Exceptions;
using FPTU_ELibrary.Application.Extensions;
using FPTU_ELibrary.Application.Utils;
using FPTU_ELibrary.Application.Validations;
using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using FPTU_ELibrary.Domain.Specifications;
using MapsterMapper;
using Serilog;

namespace FPTU_ELibrary.Application.Services;

public class LibraryItemReviewService : GenericService<LibraryItemReview, LibraryItemReviewDto, int>,
    ILibraryItemReviewService<LibraryItemReviewDto>
{
    private readonly IUserService<UserDto> _userSvc;

    public LibraryItemReviewService(
        IUserService<UserDto> userSvc,
        ISystemMessageService msgService, 
        IUnitOfWork unitOfWork, 
        IMapper mapper, 
        ILogger logger) : base(msgService, unitOfWork, mapper, logger)
    {
        _userSvc = userSvc;
    }
    
    public async Task<IServiceResult> GetItemReviewByEmailAsync(string email, int libraryItemId)
    {
        try
        {
            // Retrieve user by email
            var userDto = (await _userSvc.GetByEmailAsync(email)).Data as UserDto;
            if (userDto == null)
            {
                // Mark as authentication required to access this feature
                return new ServiceResult(
                    resultCode: ResultCodeConst.Auth_Warning0013,
                    message: await _msgService.GetMessageAsync(ResultCodeConst.Auth_Warning0013));       
            }
            
            // Build spec
            var baseSpec = new BaseSpecification<LibraryItemReview>(lir => 
                lir.UserId == userDto.UserId &&
                lir.LibraryItemId == libraryItemId);
            // Retrieve with spec
            var existingEntity = await _unitOfWork.Repository<LibraryItemReview, int>().GetWithSpecAsync(baseSpec);
            if (existingEntity == null)
            {
                // Mark as data not found or empty
                return new ServiceResult(
                    resultCode: ResultCodeConst.SYS_Warning0004,
                    message: await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004));
            }
            
            // Get data successfully
            return new ServiceResult(
                resultCode: ResultCodeConst.SYS_Success0002,
                message: await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002),
                data: _mapper.Map<LibraryItemReviewDto>(existingEntity));
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process get item review by email");
        }
    }

    public async Task<IServiceResult> ReviewItemAsync(string email, LibraryItemReviewDto dto)
    {
        try
        {
            // Retrieve user by email
            var userDto = (await _userSvc.GetByEmailAsync(email)).Data as UserDto;
            if (userDto == null)
            {
                // Mark as authentication required to access this feature
                return new ServiceResult(
                    resultCode: ResultCodeConst.Auth_Warning0013,
                    message: await _msgService.GetMessageAsync(ResultCodeConst.Auth_Warning0013));
            }

            // Validate inputs using the generic validator
            var validationResult = await ValidatorExtensions.ValidateAsync(dto);
            // Check for valid validations
            if (validationResult != null && !validationResult.IsValid)
            {
                // Convert ValidationResult to ValidationProblemsDetails.Errors
                var errors = validationResult.ToProblemDetails().Errors;
                throw new UnprocessableEntityException("Invalid Validations", errors);
            }

            // Build spec
            var baseSpec = new BaseSpecification<LibraryItemReview>(lir =>
                lir.UserId == userDto.UserId &&
                lir.LibraryItemId == dto.LibraryItemId);
            // Retrieve with spec
            var existingEntity = await _unitOfWork.Repository<LibraryItemReview, int>().GetWithSpecAsync(baseSpec);
            if (existingEntity == null)
            {
                // Current local datetime
                var currentLocalDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
                    // Vietnam timezone
                    TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));

                // Add required fields
                dto.CreateDate = currentLocalDateTime;
                dto.UserId = userDto.UserId;
                
                // Process add new library item review
                await _unitOfWork.Repository<LibraryItemReview, int>().AddAsync(_mapper.Map<LibraryItemReview>(dto));
            }
            else
            {
                // Process update existing item review
                existingEntity.RatingValue = dto.RatingValue;
                existingEntity.ReviewText = dto.ReviewText;
            }
            
            // Save DB
            var isSaved = await _unitOfWork.SaveChangesAsync() > 0;
            if (isSaved)
            {
                // Msg: You have successfully rated the item
                return new ServiceResult(
                    resultCode: ResultCodeConst.LibraryItem_Success0005,
                    message: await _msgService.GetMessageAsync(ResultCodeConst.LibraryItem_Success0005));
            }
            
            // Msg: Failed to rating for the item
            return new ServiceResult(
                resultCode: ResultCodeConst.LibraryItem_Fail0005,
                message: await _msgService.GetMessageAsync(ResultCodeConst.LibraryItem_Fail0005));
        }
        catch (UnprocessableEntityException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process review item by email");
        }
    }

    public async Task<IServiceResult> DeleteAsync(string email, int libraryItemId)
    {
        try
        {
            // Determine current system lang
            var lang = (SystemLanguage?) EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;
            
            // Retrieve user by email
            var userDto = (await _userSvc.GetByEmailAsync(email)).Data as UserDto;
            if (userDto == null)
            {
                // Mark as authentication required to access this feature
                return new ServiceResult(
                    resultCode: ResultCodeConst.Auth_Warning0013,
                    message: await _msgService.GetMessageAsync(ResultCodeConst.Auth_Warning0013));
            }
            
            // Build spec
            var baseSpec = new BaseSpecification<LibraryItemReview>(lir =>
                lir.UserId == userDto.UserId &&
                lir.LibraryItemId == libraryItemId);
            // Retrieve with spec
            var existingEntity = await _unitOfWork.Repository<LibraryItemReview, int>().GetWithSpecAsync(baseSpec);
            if (existingEntity == null)
            {
                // Not found {0}
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, isEng ? "review history" : "lịch sử đánh giá"));
            }
            
            // Process remove
            await _unitOfWork.Repository<LibraryItemReview, int>().DeleteAsync(existingEntity.ReviewId);
            // Save DB
            var isSaved = await _unitOfWork.SaveChangesAsync() > 0;
            if (isSaved)
            {
                // Msg: Delete successfully
                return new ServiceResult(ResultCodeConst.SYS_Success0004,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0004));
            }

            // Msg: Failed to delete
            return new ServiceResult(ResultCodeConst.SYS_Fail0004,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0004));
        }
        catch (UnprocessableEntityException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process update library item review");
        }
    }
}