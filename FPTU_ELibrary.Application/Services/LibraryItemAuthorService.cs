using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Dtos.Authors;
using FPTU_ELibrary.Application.Dtos.LibraryItems;
using FPTU_ELibrary.Application.Exceptions;
using FPTU_ELibrary.Application.Extensions;
using FPTU_ELibrary.Application.Utils;
using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using FPTU_ELibrary.Domain.Specifications;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace FPTU_ELibrary.Application.Services;

public class LibraryItemAuthorService : GenericService<LibraryItemAuthor, LibraryItemAuthorDto, int>,
    ILibraryItemAuthorService<LibraryItemAuthorDto>
{
    private readonly IAuthorService<AuthorDto> _authorService;
    private readonly ILibraryItemService<LibraryItemDto> _itemService;

    public LibraryItemAuthorService(
        // Lazy services
        IAuthorService<AuthorDto> authorService,
        ILibraryItemService<LibraryItemDto> itemService,
        ISystemMessageService msgService,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger logger) : base(msgService, unitOfWork, mapper, logger)
    {
        _authorService = authorService;
        _itemService = itemService;
    }

    public async Task<IServiceResult> GetFirstByLibraryItemIdAsync(int libraryItemId)
    {
        try
        {
            // Determine current system lang
            var lang = (SystemLanguage?) EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;
		    
            // Check exist lib item id 
            var isExistItem = (await _itemService.AnyAsync(x => x.LibraryItemId == libraryItemId)).Data is true;
            if (!isExistItem)
            {
                // Not found {0}
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, isEng ? "library item" : "tài liệu"));
            }
			
            // Build spec
            var libAuthorSpec = new BaseSpecification<LibraryItemAuthor>(lia => lia.LibraryItemId == libraryItemId);
            // Apply include
            libAuthorSpec.ApplyInclude(q => q.Include(lia => lia.Author));
            // Retrieve with spec
            var libAuthor = await _unitOfWork.Repository<LibraryItemAuthor, int>().GetWithSpecAsync(libAuthorSpec);
			
            // Response author information (if any)
            if (libAuthor != null)
            {
                return new ServiceResult(ResultCodeConst.SYS_Success0002,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), 
                    _mapper.Map<LibraryItemAuthorDto>(libAuthor));
            }
			
            // Not found {0}
            var msg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
            return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                StringUtils.Format(msg, isEng ? "any author in library item" : "tác giả nào gắn với tài liệu"));
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process get first author by library item id");
        }
    }
    
    public async Task<IServiceResult> AddAuthorToLibraryItemAsync(int libraryItemId, int authorId)
    {
        try
        {
            // Determine current system lang
            var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;

            // Check exist library item
            var isExistItem =
                (await _itemService.AnyAsync(x => x.LibraryItemId == libraryItemId)).Data is true;
            // Check exist author
            var isExistAuthor =
                (await _authorService.AnyAsync(x => x.AuthorId == authorId)).Data is true;
            if (!isExistItem || !isExistAuthor) // Not found library item or author
            {
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg,
                        isEng ? "library item or author" : "tài liệu hoặc tác giả"));
            }

            // Retrieve library item author entity
            var editionAuthorEntity = await _unitOfWork.Repository<LibraryItemAuthor, int>()
                .GetWithSpecAsync(new BaseSpecification<LibraryItemAuthor>(
                    x => x.LibraryItemId == libraryItemId && x.AuthorId == authorId));
            if (editionAuthorEntity != null)
            {
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0003);
                var customMsg = StringUtils.Format(errMsg,
                    isEng ? "add author" : "thêm tác giả",
                    isEng ? "author" : "tác giả");
                return new ServiceResult(ResultCodeConst.SYS_Warning0003, customMsg);
            }

            // Process add author to book edition
            await _unitOfWork.Repository<LibraryItemAuthor, int>().AddAsync(
                new LibraryItemAuthor()
                {
                    LibraryItemId = libraryItemId,
                    AuthorId = authorId
                });

            // Save to DB
            var rowEffected = await _unitOfWork.SaveChangesAsync();
            if (rowEffected == 0)
            {
                // Mark as fail to create
                return new ServiceResult(ResultCodeConst.SYS_Fail0001,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0001));
            }

            // Mark as create success
            return new ServiceResult(ResultCodeConst.SYS_Success0001,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0001));
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process add author to library item");
        }
    }

    public async Task<IServiceResult> AddRangeAuthorToLibraryItemAsync(int libraryItemId, int[] authorIds)
    {
        try
        {
            // Determine current system lang
            var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;

            // Check exist library item
            var isExistItem =
                (await _itemService.AnyAsync(x => x.LibraryItemId == libraryItemId)).Data is true;
            if (!isExistItem) // Not found item
            {
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg,
                        isEng ? "library item" : "tài liệu"));
            }

            // Initialize custom errors
            var customErrors = new Dictionary<string, string[]>();
            // Check exist author(s)
            for (int i = 0; i < authorIds.Length; ++i)
            {
                var isExistAuthor = (await _authorService.AnyAsync(a => a.AuthorId == authorIds[i])).Data is true;
                if (!isExistAuthor)
                {
                    // Add error
                    customErrors.Add($"authors[{i}]", [isEng ? "Not found author" : "Không tìm thấy tác giả"]);
                }
            }

            // Retrieve library item author entities
            var itemAuthorEntities = await _unitOfWork.Repository<LibraryItemAuthor, int>()
                .GetAllWithSpecAsync(new BaseSpecification<LibraryItemAuthor>(
                    x => x.LibraryItemId == libraryItemId && authorIds.Contains(x.AuthorId)));
            // Convert to list 
            var itemAuthorList = itemAuthorEntities.ToList();
            if (itemAuthorList.Any())
            {
                for (int i = 0; i < itemAuthorList.Count; ++i)
                {
                    var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0003);
                    var customMsg = StringUtils.Format(errMsg,
                        isEng ? "add author" : "thêm tác giả",
                        isEng ? "author" : "tác giả");
                    // Add error
                    customErrors.Add($"authors[{i}]", [customMsg]);
                }
            }

            // Check exist any errors
            if (customErrors.Any())
            {
                throw new UnprocessableEntityException("Invalid data", customErrors);
            }

            // Process add author(s) to library item
            await _unitOfWork.Repository<LibraryItemAuthor, int>().AddRangeAsync(
                authorIds.Select(a => new LibraryItemAuthor()
                {
                    LibraryItemId = libraryItemId,
                    AuthorId = a
                }).ToList());

            // Save to DB
            var rowEffected = await _unitOfWork.SaveChangesAsync();
            if (rowEffected == 0)
            {
                // Mark as fail to create
                return new ServiceResult(ResultCodeConst.SYS_Fail0001,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0001));
            }

            // Mark as create success
            return new ServiceResult(ResultCodeConst.SYS_Success0001,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0001));
        }
        catch (UnprocessableEntityException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process add range author to library item");
        }
    }

    public async Task<IServiceResult> DeleteAuthorFromLibraryItemAsync(int libraryItemId, int authorId)
    {
        try
        {
            // Determine current system lang
            var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;

            // Check exist library item
            var isExistItem =
                (await _itemService.AnyAsync(x => x.LibraryItemId == libraryItemId)).Data is true;
            // Check exist author
            var isExistAuthor =
                (await _authorService.AnyAsync(x => x.AuthorId == authorId)).Data is true;
            if (!isExistItem || !isExistAuthor) // Not found library item or author
            {
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg,
                        isEng ? "library item or author" : "tài liệu hoặc tác giả"));
            }

            // Retrieve book edition author entity
            var itemAuthorEntity = await _unitOfWork.Repository<LibraryItemAuthor, int>()
                .GetWithSpecAsync(new BaseSpecification<LibraryItemAuthor>(
                    x => x.LibraryItemId == libraryItemId && x.AuthorId == authorId));
            if (itemAuthorEntity == null)
            {
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg,
                        isEng ? "author in the library item" : "tác giả trong tài liệu"));
            }

            // Process add author to library item
            await _unitOfWork.Repository<LibraryItemAuthor, int>().DeleteAsync(itemAuthorEntity.LibraryItemAuthorId);

            // Save to DB
            var rowEffected = await _unitOfWork.SaveChangesAsync();
            if (rowEffected == 0)
            {
                // Mark as fail to create
                return new ServiceResult(ResultCodeConst.SYS_Fail0004,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0004));
            }

            // Mark as delete success
            return new ServiceResult(ResultCodeConst.SYS_Success0004,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0004));
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process delete author from library item");
        }
    }

    public async Task<IServiceResult> DeleteRangeAuthorFromLibraryItemAsync(int libraryItemId, int[] authorIds)
    {
        try
        {
            // Determine current system lang
            var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;

            // Check exist library item
            var isExistItem =
                (await _itemService.AnyAsync(x => x.LibraryItemId == libraryItemId)).Data is true;
            if (!isExistItem) // Not found library item
            {
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg,
                        isEng ? "library item" : "tài liệu"));
            }

            // Initialize custom errors
            var customErrors = new Dictionary<string, string[]>();
            // Check exist author(s)
            for (int i = 0; i < authorIds.Length; ++i)
            {
                var isExistAuthor = (await _authorService.AnyAsync(a => a.AuthorId == authorIds[i])).Data is true;
                if (!isExistAuthor)
                {
                    // Add error
                    customErrors.Add($"authors[{i}]", [isEng ? "Not found author" : "Không tìm thấy tác giả"]);
                }
            }

            // Check exist any errors
            if (customErrors.Any())
            {
                throw new UnprocessableEntityException("Invalid data", customErrors);
            }

            // Retrieve library item author entities
            var itemAuthorEntities = await _unitOfWork.Repository<LibraryItemAuthor, int>()
                .GetAllWithSpecAsync(new BaseSpecification<LibraryItemAuthor>(
                    x => x.LibraryItemId == libraryItemId && authorIds.Contains(x.AuthorId)));
            // Convert to list ids
            var toDeleteIds = itemAuthorEntities.Select(x => x.LibraryItemAuthorId).ToArray();
            if (!toDeleteIds.Any())
            {
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, isEng ? "any author match" : "tác giả trong tài liệu"));
            }

            // Progress delete range 
            await _unitOfWork.Repository<LibraryItemAuthor, int>().DeleteRangeAsync(toDeleteIds);

            // Save to DB
            var rowEffected = await _unitOfWork.SaveChangesAsync();
            if (rowEffected == 0)
            {
                // Mark as fail to create
                return new ServiceResult(ResultCodeConst.SYS_Fail0004,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0004));
            }

            // Mark as delete success
            return new ServiceResult(ResultCodeConst.SYS_Success0004,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0004));
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process delete range author from library item");
        }
    }

    public async Task<IServiceResult> DeleteRangeWithoutSaveChangesAsync(int[] bookEditionAuthorIds)
    {
        try
        {
            // Build specification
            var baseSpec = new BaseSpecification<LibraryItemAuthor>(x =>
                bookEditionAuthorIds.Contains(x.LibraryItemAuthorId));
            // Retrieve all match
            var bookEditionAuthorEntities = await _unitOfWork.Repository<LibraryItemAuthor, int>()
                .GetAllWithSpecAsync(baseSpec);
            // Convert to list
            var editionAuthorList = bookEditionAuthorEntities.ToList();
            if (!editionAuthorList.Any())
            {
                return new ServiceResult(ResultCodeConst.SYS_Fail0004,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0004));
            }

            // Progress delete range
            await _unitOfWork.Repository<LibraryItemAuthor, int>().DeleteRangeAsync(
                editionAuthorList.Select(x => x.LibraryItemAuthorId).ToArray());

            // Mark as delete without save success
            return new ServiceResult(ResultCodeConst.SYS_Success0004,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0004));
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process delete range author from library item");
        }
    }
}