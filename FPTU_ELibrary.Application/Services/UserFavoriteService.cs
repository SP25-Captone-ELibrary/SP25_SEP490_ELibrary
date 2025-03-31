using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Dtos.LibraryItems;
using FPTU_ELibrary.Application.Extensions;
using FPTU_ELibrary.Application.Utils;
using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using FPTU_ELibrary.Domain.Specifications;
using FPTU_ELibrary.Domain.Specifications.Interfaces;
using MapsterMapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace FPTU_ELibrary.Application.Services;

public class UserFavoriteService : GenericService<UserFavorite, UserFavoriteDto, int>,
    IUserFavoriteService<UserFavoriteDto>
{
    private readonly IUserService<UserDto> _userService;
    private readonly ILibraryItemService<LibraryItemDto> _libraryItemService;

    public UserFavoriteService(ISystemMessageService msgService,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IUserService<UserDto> userService,
        ILibraryItemService<LibraryItemDto> libraryItemService,
        ILogger logger)
        : base(msgService, unitOfWork, mapper, logger)
    {
        _userService = userService;
        _libraryItemService = libraryItemService;
    }

    public async Task<IServiceResult> AddFavoriteAsync(int libraryItemId, string email)
    {
        try
        {
            // Determine current system lang
            var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;

            // Retrieve user by email
            var userDto = (await _userService.GetByEmailAsync(email)).Data as UserDto;
            if (userDto == null)
            {
                // Mark as authentication required to access this feature
                return new ServiceResult(
                    resultCode: ResultCodeConst.Auth_Warning0013,
                    message: await _msgService.GetMessageAsync(ResultCodeConst.Auth_Warning0013));
            }
            
            // Build spec
            var userFavoriteSpec = new BaseSpecification<UserFavorite>(u =>
                u.UserId == userDto.UserId && u.LibraryItemId == libraryItemId);
            // Retrieve with spec
            var userFavorite = await GetWithSpecAsync(userFavoriteSpec);
            if (userFavorite.Data != null)
            {
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0003);
                return new ServiceResult(ResultCodeConst.SYS_Warning0003, isEng 
                    ? "Library item has already existed in favorite list" 
                    : "Tài liệu đã tồn tại trong mục yêu thích");
            }

            // Current local datetime
            var currentLocalDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
                // Vietnam timezone
                TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
            
            // Initialize user favorite
            var dto = new UserFavoriteDto()
            {
                LibraryItemId = libraryItemId,
                UserId = userDto.UserId,
                CreatedAt = currentLocalDateTime
            };
            // Process add new entity            
            await _unitOfWork.Repository<UserFavorite, int>().AddAsync(_mapper.Map<UserFavorite>(dto));
            // Save DB            
            if (await _unitOfWork.SaveChangesAsync() <= 0)
            {
                // Failed to save
                return new ServiceResult(ResultCodeConst.SYS_Fail0001,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0001));
            }
                
            // Msg: Added to favorites successfully
            return new ServiceResult(ResultCodeConst.LibraryItem_Success0006,
                await _msgService.GetMessageAsync(ResultCodeConst.LibraryItem_Success0006));
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process create transaction");
        }
    }

    public async Task<IServiceResult> RemoveFavoriteAsync(int libraryItemId, string email)
    {
        try
        {
            // Determine current system lang
            var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;

            // Retrieve user by email
            var userDto = (await _userService.GetByEmailAsync(email)).Data as UserDto;
            if (userDto == null)
            {
                // Mark as authentication required to access this feature
                return new ServiceResult(
                    resultCode: ResultCodeConst.Auth_Warning0013,
                    message: await _msgService.GetMessageAsync(ResultCodeConst.Auth_Warning0013));
            }

            // Build spec
            var userFavoriteSpec = new BaseSpecification<UserFavorite>(u =>
                u.UserId == userDto.UserId && u.LibraryItemId == libraryItemId);
            // Retrieve with spec
            var existingEntity = await _unitOfWork.Repository<UserFavorite, int>().GetWithSpecAsync(userFavoriteSpec);
            if (existingEntity == null)
            {
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0003,
                    StringUtils.Format(errMsg, isEng ? "item in favorite list" : "tài liệu trong mục yêu thích"));
            }
            
            // Process delete existing entity
            await _unitOfWork.Repository<UserFavorite, int>().DeleteAsync(existingEntity.FavoriteId);

            // Save to DB
            if (await _unitOfWork.SaveChangesAsync() > 0)
            {
                // Msg: Item removed from favorites successfully
                return new ServiceResult(ResultCodeConst.LibraryItem_Success0007,
                    await _msgService.GetMessageAsync(ResultCodeConst.LibraryItem_Success0007));
            }

            // Fail to delete
            return new ServiceResult(ResultCodeConst.SYS_Fail0004,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0004));
        }
        catch (DbUpdateException ex)
        {
            if (ex.InnerException is SqlException sqlEx)
            {
                switch (sqlEx.Number)
                {
                    case 547: // Foreign key constraint violation
                        return new ServiceResult(ResultCodeConst.SYS_Fail0007,
                            await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0007));
                }
            }

            // Throw if other issues
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when progress delete data");
        }
    }

    public override async Task<IServiceResult> GetAllWithSpecAsync(ISpecification<UserFavorite> specification, bool tracked = true)
    {
        try
        {
            // Determine current system lang
            var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;

            // Try to parse specification to LibraryItemSpecification
            var itemSpecification = specification as UserFavoriteSpecification;
            
            // Check if specification is null
            if (itemSpecification == null)
            {
                return new ServiceResult(ResultCodeConst.SYS_Fail0002,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0002));
            }
            
            // Count total user favorites
            var totalFavWithSpec = await _unitOfWork.Repository<UserFavorite, int>().CountAsync(itemSpecification);
            // Count total page
            var totalPage = (int)Math.Ceiling((double)totalFavWithSpec / itemSpecification.PageSize);
            
            // Set pagination to specification after count total user favorite
            if (itemSpecification.PageIndex > totalPage
                || itemSpecification.PageIndex < 1) // Exceed total page or page index smaller than 1
            {
                itemSpecification.PageIndex = 1; // Set default to first page
            }

            // Apply pagination
            itemSpecification.ApplyPaging(
                skip: itemSpecification.PageSize * (itemSpecification.PageIndex - 1),
                take: itemSpecification.PageSize);
            
            // Retrieve all with spec
            var userFavorites = await _unitOfWork.Repository<UserFavorite, int>()
                .GetAllWithSpecAndSelectorAsync(itemSpecification, uf=> new UserFavorite()
                    {
                        FavoriteId = uf.FavoriteId,
                        // References
                        LibraryItem = new LibraryItem()
                        {
                            LibraryItemId = uf.LibraryItem.LibraryItemId,
                            Title = uf.LibraryItem.Title,
                            SubTitle = uf.LibraryItem.SubTitle,
                            Responsibility = uf.LibraryItem.Responsibility,
                            Edition = uf.LibraryItem.Edition,
                            EditionNumber = uf.LibraryItem.EditionNumber,
                            Language = uf.LibraryItem.Language,
                            OriginLanguage = uf.LibraryItem.OriginLanguage,
                            Summary = uf.LibraryItem.Summary,
                            CoverImage = uf.LibraryItem.CoverImage,
                            PublicationYear = uf.LibraryItem.PublicationYear,
                            Publisher = uf.LibraryItem.Publisher,
                            PublicationPlace = uf.LibraryItem.PublicationPlace,
                            ClassificationNumber = uf.LibraryItem.ClassificationNumber,
                            CutterNumber = uf.LibraryItem.CutterNumber,
                            Isbn = uf.LibraryItem.Isbn,
                            Ean = uf.LibraryItem.Ean,
                            EstimatedPrice = uf.LibraryItem.EstimatedPrice,
                            PageCount = uf.LibraryItem.PageCount,
                            PhysicalDetails = uf.LibraryItem.PhysicalDetails,
                            Dimensions = uf.LibraryItem.Dimensions,
                            AccompanyingMaterial = uf.LibraryItem.AccompanyingMaterial,
                            Genres = uf.LibraryItem.Genres,
                            GeneralNote = uf.LibraryItem.GeneralNote,
                            BibliographicalNote = uf.LibraryItem.BibliographicalNote,
                            TopicalTerms = uf.LibraryItem.TopicalTerms,
                            AdditionalAuthors = uf.LibraryItem.AdditionalAuthors,
                            CategoryId = uf.LibraryItem.CategoryId,
                            ShelfId = uf.LibraryItem.ShelfId,
                            GroupId = uf.LibraryItem.GroupId,
                            Status = uf.LibraryItem.Status,
                            IsDeleted = uf.LibraryItem.IsDeleted,
                            IsTrained = uf.LibraryItem.IsTrained,
                            CanBorrow = uf.LibraryItem.CanBorrow,
                            TrainedAt = uf.LibraryItem.TrainedAt,
                            CreatedAt = uf.LibraryItem.CreatedAt,
                            UpdatedAt = uf.LibraryItem.UpdatedAt,
                            UpdatedBy = uf.LibraryItem.UpdatedBy,
                            CreatedBy = uf.LibraryItem.CreatedBy,
                            // References
                            Category = uf.LibraryItem.Category,
                            Shelf = uf.LibraryItem.Shelf,
                            LibraryItemInventory = uf.LibraryItem.LibraryItemInventory,
                            LibraryItemReviews = uf.LibraryItem.LibraryItemReviews,
                            LibraryItemAuthors = uf.LibraryItem.LibraryItemAuthors.Select(ba => new LibraryItemAuthor()
                            {
                                LibraryItemAuthorId = ba.LibraryItemAuthorId,
                                LibraryItemId = ba.LibraryItemId,
                                AuthorId = ba.AuthorId,
                                Author = ba.Author
                            }).ToList()
                        }
                    });
            var userFavoriteList = userFavorites.ToList();
            if (userFavoriteList.Any())
            {
                // Convert to dto collection
                var favoriteDtos = _mapper.Map<List<UserFavoriteDto>>(userFavoriteList.ToList());

                // Pagination result 
                var paginationResultDto = new PaginatedResultDto<UserFavoriteDto>(favoriteDtos,
                    itemSpecification.PageIndex, itemSpecification.PageSize, totalPage, totalFavWithSpec);

                // Response with pagination 
                return new ServiceResult(ResultCodeConst.SYS_Success0002,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), paginationResultDto);
            }
            // Not found any data
            return new ServiceResult(ResultCodeConst.SYS_Warning0004,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004),
                // Mapping entities to dto and ignore sensitive user data
                _mapper.Map<List<UserFavoriteDto>>(userFavorites));
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process get all user favorite");
        }
    }
}