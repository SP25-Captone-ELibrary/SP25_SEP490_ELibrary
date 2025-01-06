using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Configurations;
using FPTU_ELibrary.Application.Dtos.AIServices;
using FPTU_ELibrary.Application.Utils;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Serilog;
using System.Collections.Generic;
using FPTU_ELibrary.Application.Dtos.BookEditions;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Specifications;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace FPTU_ELibrary.Application.Services.IServices;

public class OCRService : IOCRService
{
    private readonly ComputerVisionClient _client;
    private readonly ICloudinaryService _cloudinaryService;
    private readonly IBookEditionService<BookEditionDto> _bookEditionService;
    private readonly ISystemMessageService _msgService;
    private readonly ILogger _logger;
    private readonly AISettings _monitor;

    public OCRService(ILogger logger, ISystemMessageService msgService, ComputerVisionClient client,
        ICloudinaryService cloudinaryService,IBookEditionService<BookEditionDto> bookEditionService
        ,IOptionsMonitor<AISettings> monitor)
    {
        _msgService = msgService;
        _client = client;
        _cloudinaryService = cloudinaryService;
        _logger = logger;
        _monitor = monitor.CurrentValue;
        _bookEditionService = bookEditionService;
    }

    private async Task<IServiceResult> ReadFileStreamAsync(Stream imageStream)
    {
        try
        {
            var textHeaders = await _client.ReadInStreamAsync(imageStream);
            string operationLocation = textHeaders.OperationLocation;

            // Retrieve the URI where the recognized text will be stored from the Operation-Location header
            //using id, not the full url
            const int numberOfCharsInOperationId = 36;
            string operationId = operationLocation.Substring(operationLocation.Length - numberOfCharsInOperationId);

            ReadOperationResult results;
            const int maxRetries = 10;
            int retries = 0;
            const int delayMilliseconds = 1000;

            do
            {
                await Task.Delay(delayMilliseconds);
                results = await _client.GetReadResultAsync(Guid.Parse(operationId));

                retries++;
                if (retries >= maxRetries)
                {
                    throw new TimeoutException("The OCR operation timed out.");
                }
            } while (results.Status == OperationStatusCodes.Running ||
                     results.Status == OperationStatusCodes.NotStarted);

            if (results.Status != OperationStatusCodes.Succeeded)
            {
                return new ServiceResult(ResultCodeConst.SYS_Fail0002,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0002));
            }

            var textResults = results.AnalyzeResult.ReadResults;
            var extractedText = string.Join(" ", textResults.SelectMany(page => page.Lines.Select(line => line.Text)))
                .Replace("\r", "")
                .Replace("\n", " ");

            return new ServiceResult(ResultCodeConst.SYS_Success0002,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), extractedText);
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process checking create book information");
        }
    }

    public async Task<IServiceResult> CheckBookInformationAsync(CheckedBookEditionDto dto)
    {
        try
        {
            //read information in book cover
            var result = await ReadFileStreamAsync(dto.Image.OpenReadStream());
            if (result.ResultCode != ResultCodeConst.SYS_Success0002)
            {
                return result;
            }

            //prepare compare data
            List<FieldMatchInputDto> compareFields = new List<FieldMatchInputDto>()
            {
                new FieldMatchInputDto()
                {
                    FieldName = "Title",
                    Values = new List<string>() { dto.Title },
                    Weight = _monitor.TitlePercentage
                },
                new FieldMatchInputDto()
                {
                    FieldName = "Authors",
                    Values = dto.Authors,
                    Weight = _monitor.AuthorNamePercentage
                },
                new FieldMatchInputDto()
                {
                    FieldName = "Publisher",
                    Values = new List<string>() { dto.Publisher },
                    Weight = _monitor.PublisherPercentage
                }
            };
            //compare data
            var matchResult =
                StringUtils.CalculateFieldMatchScore(result.Data.ToString(), compareFields,
                    _monitor.ConfidenceThreshold);
            if (matchResult.TotalPoint < matchResult.ConfidenceThreshold)
            {
                return new ServiceResult(ResultCodeConst.AIService_Warning0001,
                    await _msgService.GetMessageAsync(ResultCodeConst.AIService_Warning0001)
                    ,matchResult);
            }
            return new ServiceResult(ResultCodeConst.AIService_Success0001,
                await _msgService.GetMessageAsync(ResultCodeConst.AIService_Success0001)
                ,matchResult);
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process checking create book information");
        }
    }

    public async Task<IServiceResult> CheckTrainingInputAsync(int bookEditionId, List<IFormFile> images)
    {
        try
        {
            var baseSpec = new BaseSpecification<BookEdition>(x => x.BookEditionId == bookEditionId);
            baseSpec.ApplyInclude(q => q.Include(x => x.BookEditionAuthors)
                .ThenInclude(ea => ea.Author));
            //get book edition
            var bookEditionResult = await _bookEditionService.GetWithSpecAsync(baseSpec);
            if (bookEditionResult.ResultCode != ResultCodeConst.SYS_Success0002)
            {
                return bookEditionResult;
            }
            var dto = (BookEditionDto)bookEditionResult.Data!;
               
            var responseData = new TrainingImageMatchResultDto
            {
                TrainingImageResult = new List<SingleImageMatchResultDto>()
            };
            foreach (var image in images)
            {
                //read information in book cover
                var result = await ReadFileStreamAsync(image.OpenReadStream());
                if (result.ResultCode != ResultCodeConst.SYS_Success0002)
                {
                    return result;
                }
                //prepare compare data
                var authors = dto.BookEditionAuthors.Where(x => x.BookEditionId == bookEditionId)
                    .Select(x => x.Author.FullName).ToList();
                List<FieldMatchInputDto> compareFields = new List<FieldMatchInputDto>()
                {
                    new FieldMatchInputDto()
                    {
                        FieldName = "Title",
                        Values = new List<string>() { dto.EditionTitle },
                        Weight = _monitor.TitlePercentage
                    },
                    new FieldMatchInputDto()
                    {
                        FieldName = "Authors",
                        Values = authors,
                        Weight = _monitor.AuthorNamePercentage
                    },
                    new FieldMatchInputDto()
                    {
                        FieldName = "Publisher",
                        Values = new List<string>() { dto.Publisher },
                        Weight = _monitor.PublisherPercentage
                    }
                };
                //compare data
                var matchResult =
                    StringUtils.CalculateFieldMatchScore(result.Data.ToString(), compareFields,
                        _monitor.ConfidenceThreshold);
                responseData.TrainingImageResult.Add(new SingleImageMatchResultDto()
                {
                    FieldPoints = matchResult.FieldPoints,
                    ConfidenceThreshold = matchResult.ConfidenceThreshold,
                    TotalPoint = matchResult.TotalPoint,
                    ImageName = image.FileName,
                });
            }

            return new ServiceResult(ResultCodeConst.AIService_Success0001,
                await _msgService.GetMessageAsync(ResultCodeConst.AIService_Success0001)
                , responseData);
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process checking training book information");
        }
    }
}