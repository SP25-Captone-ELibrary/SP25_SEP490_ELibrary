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
using FPTU_ELibrary.Domain.Specifications.Params;
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

            // Check if item was added to favorite list
            var user = await _userService.GetByEmailAsync(email);
            if (user.Data is null)
            {
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg,
                        isEng
                            ? "Cannot found user that match with email"
                            : "Không tìm thấy người dùng phù hợp với email"
                    ));
            }

            var userFavoriteSpec = new BaseSpecification<UserFavorite>(u =>
                u.UserId == (user.Data as UserDto)!.UserId && u.LibraryItemId == libraryItemId);
            var userFavorite = await GetWithSpecAsync(userFavoriteSpec);
            if (userFavorite.Data != null)
            {
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0003);
                return new ServiceResult(ResultCodeConst.SYS_Warning0003,
                    StringUtils.Format(errMsg,
                        isEng ? "favorite item" : "mục yêu thích",
                        isEng
                            ? "Item already added to favorite list"
                            : "Mục đã được thêm vào danh sách yêu thích"
                    ));
            }

            var dto = new UserFavoriteDto()
            {
                LibraryItemId = libraryItemId,
                UserId = (user.Data as UserDto)!.UserId
            };
            var entity = _mapper.Map<UserFavorite>(dto);
            await _unitOfWork.Repository<UserFavorite, int>().AddAsync(entity);
            if (await _unitOfWork.SaveChangesAsync() <= 0)
            {
                return new ServiceResult(ResultCodeConst.SYS_Fail0001,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0001));
            }

            return new ServiceResult(ResultCodeConst.SYS_Success0001,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0001), entity);
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

            // Check if item was added to favorite list
            var user = await _userService.GetByEmailAsync(email);
            if (user.Data is null)
            {
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg,
                        isEng
                            ? "Cannot found user that match with email"
                            : "Không tìm thấy người dùng phù hợp với email"
                    ));
            }

            var userFavoriteSpec = new BaseSpecification<UserFavorite>(u =>
                u.UserId == (user.Data as UserDto)!.UserId && u.LibraryItemId == libraryItemId);
            var userFavorite = await GetWithSpecAsync(userFavoriteSpec);
            if (userFavorite.Data is null)
            {
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0003,
                    StringUtils.Format(errMsg,
                        isEng
                            ? "Cannot find this item in favorite list"
                            : "Không tìm thấy sản phẩm này trong mục yêu thích"
                    ));
            }

            await _unitOfWork.Repository<UserFavorite, int>()
                .DeleteAsync((userFavorite.Data as UserFavoriteDto)!.FavoriteId);
            // Save to DB
            if (await _unitOfWork.SaveChangesAsync() > 0)
            {
                return new ServiceResult(ResultCodeConst.SYS_Success0004,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0004), true);
            }

            // Fail to delete
            return new ServiceResult(ResultCodeConst.SYS_Fail0004,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0004), false);
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

    public override async Task<IServiceResult> GetAllWithSpecAsync(ISpecification<UserFavorite> specification,
        bool tracked = true)
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
            
            // Count total library items
            var totalLibItemWithSpec = await _unitOfWork.Repository<UserFavorite, int>().CountAsync(itemSpecification);
            // Count total page
            var totalPage = (int)Math.Ceiling((double)totalLibItemWithSpec / itemSpecification.PageSize);
            
            // Set pagination to specification after count total library item
            if (itemSpecification.PageIndex > totalPage
                || itemSpecification.PageIndex < 1) // Exceed total page or page index smaller than 1
            {
                itemSpecification.PageIndex = 1; // Set default to first page
            }

            // Apply pagination
            itemSpecification.ApplyPaging(
                skip: itemSpecification.PageSize * (itemSpecification.PageIndex - 1),
                take: itemSpecification.PageSize);
            var userFavorites = await _unitOfWork.Repository<UserFavorite, int>()
                .GetAllWithSpecAndSelectorAsync(itemSpecification, uf=> new UserFavorite()
                    {
                        FavoriteId = uf.FavoriteId,
                        //References
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
                            //References
                            Category = uf.LibraryItem.Category,
                            Shelf = uf.LibraryItem.Shelf,
                            LibraryItemGroup = uf.LibraryItem.LibraryItemGroup,
                            LibraryItemInventory = uf.LibraryItem.LibraryItemInventory,
                            LibraryItemInstances = uf.LibraryItem.LibraryItemInstances,
                            LibraryItemReviews = uf.LibraryItem.LibraryItemReviews,
                            LibraryItemAuthors = uf.LibraryItem.LibraryItemAuthors.Select(ba => new LibraryItemAuthor()
                            {
                                LibraryItemAuthorId = ba.LibraryItemAuthorId,
                                LibraryItemId = ba.LibraryItemId,
                                AuthorId = ba.AuthorId,
                                Author = ba.Author
                            }).ToList()
                        }
                    }
                    );
            var enumerable = userFavorites.ToList();
            if (enumerable.Any())
            {
                // Convert to dto collection
                var itemDtos = _mapper.Map<List<UserFavoriteDto>>(enumerable.ToList());

                // Pagination result 
                var paginationResultDto = new PaginatedResultDto<UserFavoriteDto>(itemDtos,
                    itemSpecification.PageIndex, itemSpecification.PageSize, totalPage, totalLibItemWithSpec);

                // Response with pagination 
                return new ServiceResult(ResultCodeConst.SYS_Success0002,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), paginationResultDto);
            }
            // Not found any data
            return new ServiceResult(ResultCodeConst.SYS_Warning0004,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004),
                // Mapping entities to dto and ignore sensitive user data
                _mapper.Map<IEnumerable<UserFavoriteDto>>(userFavorites));

        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}