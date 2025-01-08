using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Dtos.Authors;
using FPTU_ELibrary.Application.Dtos.BookEditions;
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
using Serilog;

namespace FPTU_ELibrary.Application.Services;

public class BookEditionAuthorService : GenericService<BookEditionAuthor, BookEditionAuthorDto, int>,
    IBookEditionAuthorService<BookEditionAuthorDto>
{
    private readonly IAuthorService<AuthorDto> _authorService;
    private readonly IBookEditionService<BookEditionDto> _editionService;

    public BookEditionAuthorService(
        IBookEditionService<BookEditionDto> editionService,
        IAuthorService<AuthorDto> authorService,
        ISystemMessageService msgService, 
        IUnitOfWork unitOfWork, 
        IMapper mapper, 
        ILogger logger) : base(msgService, unitOfWork, mapper, logger)
    {
        _authorService = authorService;
        _editionService = editionService;
    }

    public async Task<IServiceResult> AddAuthorToBookEditionAsync(int bookEditionId, int authorId)
    {
        try
        {
            // Determine current system lang
            var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;
            
            // Check exist book edition
            var isExistEdition =
                (await _editionService.AnyAsync(x => x.BookEditionId == bookEditionId)).Data is true;
            // Check exist author
            var isExistAuthor =
                (await _authorService.AnyAsync(x => x.AuthorId == authorId)).Data is true;
            if (!isExistEdition || !isExistAuthor) // Not found book edition or author
            {
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, 
                        isEng ? "book edition or author" : "ấn bản hoặc tác giả"));
            }
            
            // Retrieve book edition author entity
            var editionAuthorEntity = await _unitOfWork.Repository<BookEditionAuthor, int>()
                .GetWithSpecAsync(new BaseSpecification<BookEditionAuthor>(
                    x => x.BookEditionId == bookEditionId && x.AuthorId == authorId));
            if (editionAuthorEntity != null)
            {
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0003);
                var customMsg = StringUtils.Format(errMsg,
                    isEng ? "add author" : "thêm tác giả",
                    isEng ? "author" : "tác giả");
                return new ServiceResult(ResultCodeConst.SYS_Warning0003, customMsg);
            }
            
            // Process add author to book edition
            await _unitOfWork.Repository<BookEditionAuthor, int>().AddAsync(
                new BookEditionAuthor()
                {
                    BookEditionId = bookEditionId,
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
            throw new Exception("Error invoke when process add author to book edition");
        }
    }

    public async Task<IServiceResult> AddRangeAuthorToBookEditionAsync(int bookEditionId, int[] authorIds)
    {
        try
        {
            // Determine current system lang
            var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;

            // Check exist book edition
            var isExistEdition =
                (await _editionService.AnyAsync(x => x.BookEditionId == bookEditionId)).Data is true;
            if (!isExistEdition) // Not found book edition
            {
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg,
                        isEng ? "book edition" : "ấn bản"));
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
            
            // Retrieve book edition author entities
            var editionAuthorEntities = await _unitOfWork.Repository<BookEditionAuthor, int>()
                .GetAllWithSpecAsync(new BaseSpecification<BookEditionAuthor>(
                    x => x.BookEditionId == bookEditionId && authorIds.Contains(x.AuthorId)));
            // Convert to list 
            var editionAuthorList = editionAuthorEntities.ToList();
            if (editionAuthorList.Any())
            {
                for (int i = 0; i < editionAuthorList.Count; ++i)
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
            
            // Process add author(s) to book edition
            await _unitOfWork.Repository<BookEditionAuthor, int>().AddRangeAsync(
                authorIds.Select(a => new BookEditionAuthor()
                    {
                        BookEditionId = bookEditionId,
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
            throw new Exception("Error invoke when process add range author to book edition");
        }
    }
    
    public async Task<IServiceResult> DeleteAuthorFromBookEditionAsync(int bookEditionId, int authorId)
    {
        try
        {
            // Determine current system lang
            var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;
            
            // Check exist book edition
            var isExistEdition =
                (await _editionService.AnyAsync(x => x.BookEditionId == bookEditionId)).Data is true;
            // Check exist author
            var isExistAuthor =
                (await _authorService.AnyAsync(x => x.AuthorId == authorId)).Data is true;
            if (!isExistEdition || !isExistAuthor) // Not found book edition or author
            {
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, 
                        isEng ? "book edition or author" : "ấn bản hoặc tác giả"));
            }
            
            // Retrieve book edition author entity
            var editionAuthorEntity = await _unitOfWork.Repository<BookEditionAuthor, int>()
                .GetWithSpecAsync(new BaseSpecification<BookEditionAuthor>(
                    x => x.BookEditionId == bookEditionId && x.AuthorId == authorId));
            if (editionAuthorEntity == null)
            {
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, 
                        isEng ? "author in the book edition" : "tác giả trong ấn bản"));
            }
            
            // Process add author to book edition
            await _unitOfWork.Repository<BookEditionAuthor, int>().DeleteAsync(editionAuthorEntity.BookEditionAuthorId);
            
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
            throw new Exception("Error invoke when process delete author from book edition");
        }
    }

    public async Task<IServiceResult> DeleteRangeAuthorFromBookEditionAsync(int bookEditionId, int[] authorIds)
    {
        try
        {
            // Determine current system lang
            var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;

            // Check exist book edition
            var isExistEdition =
                (await _editionService.AnyAsync(x => x.BookEditionId == bookEditionId)).Data is true;
            if (!isExistEdition) // Not found book edition
            {
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg,
                        isEng ? "book edition" : "ấn bản"));
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
            
            // Retrieve book edition author entities
            var editionAuthorEntities = await _unitOfWork.Repository<BookEditionAuthor, int>()
                .GetAllWithSpecAsync(new BaseSpecification<BookEditionAuthor>(
                    x => x.BookEditionId == bookEditionId && authorIds.Contains(x.AuthorId)));
            // Convert to list ids
            var toDeleteIds = editionAuthorEntities.Select(x => x.BookEditionAuthorId).ToArray();
            if (!toDeleteIds.Any())
            {
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, isEng ? "any author match" : "tác giả trong ấn bản"));
            }
            
            // Progress delete range 
            await _unitOfWork.Repository<BookEditionAuthor, int>().DeleteRangeAsync(toDeleteIds);
            
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
            throw new Exception("Error invoke when process delete range author from book edition");
        }
    }
    
    public async Task<IServiceResult> DeleteRangeWithoutSaveChangesAsync(int[] bookEditionAuthorIds)
    {
        try
        {
            // Build specification
            var baseSpec = new BaseSpecification<BookEditionAuthor>(x => 
                bookEditionAuthorIds.Contains(x.BookEditionAuthorId));
            // Retrieve all match
            var bookEditionAuthorEntities = await _unitOfWork.Repository<BookEditionAuthor, int>()
                .GetAllWithSpecAsync(baseSpec);
            // Convert to list
            var editionAuthorList = bookEditionAuthorEntities.ToList();
            if (!editionAuthorList.Any())
            {
                return new ServiceResult(ResultCodeConst.SYS_Fail0004,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0004));
            }
            
            // Progress delete range
            await _unitOfWork.Repository<BookEditionAuthor, int>().DeleteRangeAsync(
                editionAuthorList.Select(x => x.BookEditionAuthorId).ToArray());
            
            // Mark as delete without save success
            return new ServiceResult(ResultCodeConst.SYS_Success0004,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0004));
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process delete range author from book edition");
        }
    }
}