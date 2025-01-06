using System.Globalization;
using Azure.Core;
using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Configurations;
using FPTU_ELibrary.Application.Dtos.AIServices.Speech;
using FPTU_ELibrary.Application.Dtos.BookEditions;
using FPTU_ELibrary.Application.Dtos.Books;
using FPTU_ELibrary.Application.Elastic.Params;
using FPTU_ELibrary.Application.Services.IServices;
using FPTU_ELibrary.Application.Utils;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using FPTU_ELibrary.Domain.Specifications;
using Microsoft.AspNetCore.Http;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Serilog;

namespace FPTU_ELibrary.Application.Services;

public class VoiceService : IVoiceService
{
    private readonly SpeechConfig _speechConfig;
    private readonly IBookService<BookDto> _bookService;
    private readonly IBookEditionService<BookEditionDto> _editionService;
    private readonly ISearchService _searchService;
    private readonly ILogger _logger;
    private readonly ISystemMessageService _msgService;
    private readonly AzureSpeechSettings _monitor;

    public VoiceService(SpeechConfig speechConfig, IBookService<BookDto> bookService
        , IBookEditionService<BookEditionDto> editionService
        , ISearchService searchService, IOptionsMonitor<AzureSpeechSettings> monitor,
        ILogger logger, ISystemMessageService msgService)
    {
        _speechConfig = speechConfig;
        _bookService = bookService;
        _editionService = editionService;
        _searchService = searchService;
        _logger = logger;
        _msgService = msgService;
        _monitor = monitor.CurrentValue;
    }

    public async Task<IServiceResult> VoiceToText(IFormFile audioFile, string languageCode)
    {
        var tempFilePath = Path.GetTempFileName();
        try
        {
            using (var fileStream = new FileStream(tempFilePath, FileMode.Create))
            {
                await audioFile.CopyToAsync(fileStream);
            }

            // Create AudioConfig from the temporary WAV file
            using var audioInput = AudioConfig.FromWavFileInput(tempFilePath);
            using var recognizer = new SpeechRecognizer(_speechConfig, languageCode, audioInput);

            // Perform speech recognition
            var result = await recognizer.RecognizeOnceAsync();
            var recogniseTitle = StringUtils.SplitSpecialCharAtTheEnd(result.Text);

            // Version 1: Match with audio
            // Get match edition
            var matchedBookQuery = new BaseSpecification<BookEdition>(x => x.EditionTitle.Equals(recogniseTitle)
                                                                           || x.Book.Title.Equals(recogniseTitle));
            matchedBookQuery.ApplyInclude(q => q.Include(e => e.Book)
                .ThenInclude(b => b.BookCategories));
            var bookEdition = (BookEditionDto)(await _editionService.GetWithSpecAsync(matchedBookQuery)).Data!;

            var recognisedResponse = new RecognisedItemDto<BookEditionDto>()
            {
                MatchedItem = bookEdition,
                RelatedItemsDetails = new List<RelatedItemDto<BookEditionDto>>()
            };

            // Get related editions by author name and category
            // Same Author case:
            var sameAuthorEditionsQuery = new BaseSpecification<BookEdition>(be => be.BookEditionAuthors.Any(ba =>
                be.BookEditionAuthors
                    .Where(tba => tba.BookEditionId == bookEdition.BookEditionId)
                    .Select(tba => tba.AuthorId)
                    .Contains(ba.AuthorId)));
            sameAuthorEditionsQuery.ApplyInclude(q => q
                .Include(be => be.Book)
                .ThenInclude(b => b.BookCategories)
                .ThenInclude(c => c.Category)
                .Include(be => be.BookEditionAuthors)
                .ThenInclude(bea => bea.Author)
            );
            var sameAuthorEditions =
                (List<BookEditionDto>)(await _editionService.GetAllWithSpecAsync(sameAuthorEditionsQuery)).Data!;
            recognisedResponse.RelatedItemsDetails.Add(new RelatedItemDto<BookEditionDto>()
            {
                RelatedProperty = nameof(Author),
                RelatedItems = sameAuthorEditions
            });

            //Same Categories case:

            var sameCategoryEditionsQuery = new BaseSpecification<BookEdition>(be =>
                be.BookEditionId != bookEdition.BookEditionId &&
                be.Book.BookCategories
                    .Any(bc => 
                        bc.CategoryId == be.Book.BookCategories
                            .Where(bcInner =>
                                bcInner.Book.BookEditions
                                    .Any(tbe => tbe.BookEditionId == bookEdition.BookEditionId))
                            .Select(bcInner => bcInner.CategoryId)
                            .FirstOrDefault()
                    )
            );

// Apply Includes for Book, BookCategories, Category, and BookEditionAuthors
            sameCategoryEditionsQuery.ApplyInclude(q => q
                .Include(be => be.Book)
                .ThenInclude(b => b.BookCategories)
                .ThenInclude(c => c.Category)
                .Include(be => be.BookEditionAuthors)
                .ThenInclude(bea => bea.Author)
            );

// Retrieve the data using the specification
            var sameCategoryEditions = (List<BookEditionDto>)(await _editionService.GetAllWithSpecAsync(sameCategoryEditionsQuery)).Data!;
            recognisedResponse.RelatedItemsDetails.Add(new RelatedItemDto<BookEditionDto>()
            {
                RelatedItems = sameCategoryEditions,
                RelatedProperty = nameof(Category)
            });
            return new ServiceResult(ResultCodeConst.SYS_Success0002
                , await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), recognisedResponse);
            /*
            // using elasticsearch for support best suitable book
            var searchResultWithTitle = await _searchService.SearchBookAsync(new SearchBookParameters(
            {
                 SearchText = recogniseTitle
             });
            */
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when Train Book Model");
        }
    }

    public async Task<IServiceResult> GetLanguages()
    {
        try
        {
            List<SpeechLanguagesDto> availableLanguages = new List<SpeechLanguagesDto>();
            availableLanguages = _monitor.Languages
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(langCode =>
                {
                    var trimmedCode = langCode.Trim();
                    try
                    {
                        var culture = new CultureInfo(trimmedCode);
                        return new SpeechLanguagesDto()
                        {
                            LanguageCode = trimmedCode,
                            LanguageName = culture.DisplayName
                        };
                    }
                    catch (CultureNotFoundException)
                    {
                        return new SpeechLanguagesDto()
                        {
                            LanguageCode = trimmedCode,
                            LanguageName = "Unknown Language"
                        };
                    }
                })
                .ToList();
            return new ServiceResult(ResultCodeConst.SYS_Success0002,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), availableLanguages);
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when Train Book Model");
        }
    }
}