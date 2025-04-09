using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Dtos.AIServices;
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
using FPTU_ELibrary.Domain.Specifications.Interfaces;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
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

    public override async Task<IServiceResult> CreateAsync(AITrainingSessionDto dto)
    {
        try
        {
            // Validate inputs using the generic validator
            var validationResult = await ValidatorExtensions.ValidateAsync(dto);
            // Check for valid validations
            if (validationResult != null && !validationResult.IsValid)
            {
                // Convert ValidationResult to ValidationProblemsDetails.Errors
                var errors = validationResult.ToProblemDetails().Errors;
                throw new UnprocessableEntityException("Invalid Validations", errors);
            }
            
            // Mapping to entity
            var entity = _mapper.Map<AITrainingSession>(dto);
            // Process add new entity
            await _unitOfWork.Repository<AITrainingSession, int>().AddAsync(entity);
            // Save DB
            if (await _unitOfWork.SaveChangesAsync() > 0)
            {
                return new ServiceResult(ResultCodeConst.SYS_Success0001,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0001), 
                    _mapper.Map<AITrainingSessionDto>(entity));
            }
            
            return new ServiceResult(ResultCodeConst.SYS_Fail0001,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0001));
        }
        catch (UnprocessableEntityException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process create AI training session");
        }
    }

    public override async Task<IServiceResult> GetByIdAsync(int id)
    {
        try
        {
            // Build spec
            var baseSpec = new BaseSpecification<AITrainingSession>(x => x.TrainingSessionId == id);
            // Retrieve with spec
            var existingEntity = await _unitOfWork.Repository<AITrainingSession, int>()
                .GetWithSpecAndSelectorAsync(baseSpec, selector: s => new AITrainingSession()
                {
                    TrainingSessionId = s.TrainingSessionId,
                    Model = s.Model,
                    TotalTrainedItem = s.TotalTrainedItem,
                    TotalTrainedTime = s.TotalTrainedTime,
                    TrainingStatus = s.TrainingStatus,
                    ErrorMessage = s.ErrorMessage,
                    TrainingPercentage = s.TrainingPercentage,
                    TrainDate = s.TrainDate,
                    TrainBy = s.TrainBy,
                    TrainingDetails = s.TrainingDetails.Select(td => new AITrainingDetail()
                    {
                        TrainingDetailId = td.TrainingDetailId,
                        TrainingSessionId = td.TrainingSessionId,
                        LibraryItemId = td.LibraryItemId,
                        TrainingSession = td.TrainingSession,
                        TrainingImages = td.TrainingImages,
                        LibraryItem = new LibraryItem()
                        {
                            LibraryItemId = td.LibraryItem.LibraryItemId,
                            Title = td.LibraryItem.Title,
                            SubTitle = td.LibraryItem.SubTitle,
                            Responsibility = td.LibraryItem.Responsibility,
                            Edition = td.LibraryItem.Edition,
                            EditionNumber = td.LibraryItem.EditionNumber,
                            Language = td.LibraryItem.Language,
                            OriginLanguage = td.LibraryItem.OriginLanguage,
                            Summary = td.LibraryItem.Summary,
                            CoverImage = td.LibraryItem.CoverImage,
                            PublicationYear = td.LibraryItem.PublicationYear,
                            Publisher = td.LibraryItem.Publisher,
                            PublicationPlace = td.LibraryItem.PublicationPlace,
                            ClassificationNumber = td.LibraryItem.ClassificationNumber,
                            CutterNumber = td.LibraryItem.CutterNumber,
                            Isbn = td.LibraryItem.Isbn,
                            Ean = td.LibraryItem.Ean,
                            EstimatedPrice = td.LibraryItem.EstimatedPrice,
                            PageCount = td.LibraryItem.PageCount,
                            PhysicalDetails = td.LibraryItem.PhysicalDetails,
                            Dimensions = td.LibraryItem.Dimensions,
                            AccompanyingMaterial = td.LibraryItem.AccompanyingMaterial,
                            Genres = td.LibraryItem.Genres,
                            GeneralNote = td.LibraryItem.GeneralNote,
                            BibliographicalNote = td.LibraryItem.BibliographicalNote,
                            TopicalTerms = td.LibraryItem.TopicalTerms,
                            AdditionalAuthors = td.LibraryItem.AdditionalAuthors,
                            CategoryId = td.LibraryItem.CategoryId,
                            ShelfId = td.LibraryItem.ShelfId,
                            GroupId = td.LibraryItem.GroupId,
                            Status = td.LibraryItem.Status,
                            IsDeleted = td.LibraryItem.IsDeleted,
                            IsTrained = td.LibraryItem.IsTrained,
                            CanBorrow = td.LibraryItem.CanBorrow,
                            TrainedAt = td.LibraryItem.TrainedAt,
                            CreatedAt = td.LibraryItem.CreatedAt,
                            UpdatedAt = td.LibraryItem.UpdatedAt,
                            UpdatedBy = td.LibraryItem.UpdatedBy,
                            CreatedBy = td.LibraryItem.CreatedBy,
                            // References
                            Category = td.LibraryItem.Category,
                            Shelf = td.LibraryItem.Shelf,
                            LibraryItemGroup = td.LibraryItem.LibraryItemGroup,
                            LibraryItemInventory = td.LibraryItem.LibraryItemInventory,
                            LibraryItemInstances = td.LibraryItem.LibraryItemInstances,
                            LibraryItemReviews = td.LibraryItem.LibraryItemReviews,
                            LibraryItemAuthors = td.LibraryItem.LibraryItemAuthors.Select(ba => new LibraryItemAuthor()
                            {
                                LibraryItemAuthorId = ba.LibraryItemAuthorId,
                                LibraryItemId = ba.LibraryItemId,
                                AuthorId = ba.AuthorId,
                                Author = ba.Author
                            }).ToList()
                        }
                    }).ToList()
                });
            if (existingEntity == null)
            {
                return new ServiceResult(ResultCodeConst.SYS_Warning0004,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004));
            }

            return new ServiceResult(ResultCodeConst.SYS_Success0002,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002),
                _mapper.Map<AITrainingSessionDto>(existingEntity));
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when progress get data detail");
        }
    }

    public override async Task<IServiceResult> GetAllWithSpecAsync(ISpecification<AITrainingSession> spec,
        bool tracked = true)
    {
        try
        {
            // Try to parse specification to UserSpecification
            var userSpec = spec as AITrainingSessionSpecification;
            // Check if specification is null
            if (userSpec == null)
            {
                return new ServiceResult(ResultCodeConst.SYS_Fail0002,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0002));
            }

            // Count total sessions
            var totalSessions = await _unitOfWork.Repository<AITrainingSession, int>().CountAsync(userSpec);
            // Count total page
            var totalPage = (int)Math.Ceiling(totalSessions / (double)userSpec.PageSize);

            // Set pagination to specification after count total users 
            if (userSpec.PageIndex > totalPage
                || userSpec.PageIndex < 1) // Exceed total page or page index smaller than 1
            {
                userSpec.PageIndex = 1; // Set default to first page
            }

            // Apply pagination
            userSpec.ApplyPaging(
                skip: userSpec.PageSize * (userSpec.PageIndex - 1),
                take: userSpec.PageSize);

            var entities = await _unitOfWork.Repository<AITrainingSession, int>().GetAllWithSpecAsync(userSpec);

            if (entities.Any())
            {
                // Convert to Dto
                var sessions = _mapper.Map<IEnumerable<AITrainingSessionDto>>(entities);
                // Pagination result
                var paginationResultDto = new PaginatedResultDto<AITrainingSessionDto>(sessions,
                    userSpec.PageIndex, userSpec.PageSize, totalPage, totalSessions);

                return new ServiceResult(ResultCodeConst.SYS_Success0002,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002),
                    paginationResultDto);
            }

            // Not found any data
            return new ServiceResult(ResultCodeConst.SYS_Warning0004,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004),
                _mapper.Map<IEnumerable<AITrainingSessionDto>>(entities)
            );
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when progress get all data");
        }
    }

    public async Task<IServiceResult> UpdateSuccessSessionStatus(int sessionId, bool isSuccess, string? errorMessage = null)
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
            existingEntity.ErrorMessage = errorMessage;
            if (errorMessage != null) existingEntity.TrainingPercentage = 0;
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
    
    public async Task<IServiceResult> UpdatePercentage(int sessionId, int? percentage)
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
            existingEntity.TrainingPercentage = percentage;
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
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when progress update percentage for session");
        }
    }
}