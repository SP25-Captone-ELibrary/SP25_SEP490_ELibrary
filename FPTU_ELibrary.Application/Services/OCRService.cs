using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Configurations;
using FPTU_ELibrary.Application.Dtos.AIServices;
using FPTU_ELibrary.Application.Utils;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using Microsoft.Extensions.Options;
using Serilog;
using FPTU_ELibrary.Application.Dtos.LibraryItems;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Specifications;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Exception = System.Exception;

namespace FPTU_ELibrary.Application.Services.IServices;

public class OCRService : IOCRService
{
    private readonly ComputerVisionClient _client;
    private readonly ICloudinaryService _cloudinaryService;
    private readonly ILibraryItemService<LibraryItemDto> _libraryItemService;
    private readonly ISystemMessageService _msgService;
    private readonly ILogger _logger;
    private readonly AISettings _monitor;

    public OCRService(ILogger logger, ISystemMessageService msgService, ComputerVisionClient client,
        ICloudinaryService cloudinaryService, ILibraryItemService<LibraryItemDto> libraryItemService
        , IOptionsMonitor<AISettings> monitor)
    {
        _msgService = msgService;
        _client = client;
        _cloudinaryService = cloudinaryService;
        _logger = logger;
        _monitor = monitor.CurrentValue;
        _libraryItemService = libraryItemService;
    }

    private async Task<IServiceResult> ReadRawFileSteam(Stream imageStream)
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

            return new ServiceResult(ResultCodeConst.SYS_Success0002,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), results);
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process checking create book information");
        }
    }

    private async Task<IServiceResult> ReadFileStreamAsync(Stream imageStream)
    {
        try
        {
            var results = await ReadRawFileSteam(imageStream);
            if (results.ResultCode != ResultCodeConst.SYS_Success0002)
            {
                return new ServiceResult(ResultCodeConst.SYS_Fail0002,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0002));
            }

            var textResults = ((ReadOperationResult)results.Data).AnalyzeResult.ReadResults;
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

    public async Task<IServiceResult> CheckBookInformationAsync(CheckedItemDto dto)
    {
        try
        {
            var acceptableImage = new List<MatchResultDto>();
            //read information in book cover
            foreach (var image in dto.Images)
            {
                var result = await ReadFileStreamAsync(image.OpenReadStream());
                if (result.ResultCode != ResultCodeConst.SYS_Success0002)
                {
                    return result;
                }

                //prepare compare data
                // create base compare fields
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
                        FieldName = "Publisher",
                        Values = new List<string>() { dto.Publisher },
                        Weight = _monitor.PublisherPercentage
                    }
                };
                // check if SubTitle is existed
                if (dto.SubTitle is not null)
                {
                    compareFields.First(cf => cf.FieldName.Equals("Title"))
                        .Values.Add(dto.SubTitle);
                }

                // check if GeneralNote is existed
                // compareFields.Add(new FieldMatchInputDto()
                // {
                //     FieldName = "Authors",
                //     Values = dto.Authors,
                //     Weight = _monitor.AuthorNamePercentage
                // });
                // check if GeneralNote is existed
                // if (dto.GeneralNote is not null)
                // {
                //     compareFields.Add(new FieldMatchInputDto()
                //     {
                //         FieldName = "Authors",
                //         Values = new List<string>() { dto.GeneralNote },
                //         Weight = _monitor.AuthorNamePercentage
                //     });
                // }

                var matchResult =
                    StringUtils.CalculateFieldMatchScore(result.Data.ToString(), compareFields,
                        _monitor.ConfidenceThreshold, _monitor.MinFieldThreshold);
                matchResult.ImageName = image.FileName;
                acceptableImage.Add(matchResult);
            }

            return new ServiceResult(ResultCodeConst.AIService_Success0001,
                await _msgService.GetMessageAsync(ResultCodeConst.AIService_Success0001)
                , acceptableImage);
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process checking create book information");
        }
    }

    public async Task<IServiceResult> OcrDetailAsync(IFormFile image, int bestItemId)
    {
        try
        {
            // Dictionary to store data for response
            Dictionary<int, MatchResultDto> fieldMatchedResult = new Dictionary<int, MatchResultDto>();

            // Read raw detail from image
            var rawOrcResult = await ReadRawFileSteam(image.OpenReadStream());
            if (rawOrcResult.ResultCode != ResultCodeConst.SYS_Success0002)
            {
                return rawOrcResult;
            }

            var textResults = (rawOrcResult.Data as ReadOperationResult).AnalyzeResult.ReadResults;
            string ocrValue = string.Join(" ", textResults.SelectMany(page => page.Lines.Select(line => line.Text)))
                .Replace("\r", "")
                .Replace("\n", " ");
            var processedOcrValue = textResults.SelectMany(page =>
                page.Lines.Select(line =>
                    line.Text.Replace("\r", "")
                        .Replace("\n", " "))).ToList();
            var itemBaseSpec = new BaseSpecification<LibraryItem>(x => x.LibraryItemId == bestItemId);
            itemBaseSpec.ApplyInclude(q => q.Include(x => x.LibraryItemAuthors)
                .ThenInclude(ea => ea.Author));
            var libraryItem = await _libraryItemService.GetWithSpecAsync(itemBaseSpec);
            if (libraryItem.ResultCode != ResultCodeConst.SYS_Success0002)
            {
                return libraryItem;
            }

            var dto = (LibraryItemDto)libraryItem.Data!;
            var mainAuthor = dto.LibraryItemAuthors.Select(x => x.Author.FullName).ToList();
            for (var i = 0; i < processedOcrValue.Count; i++)
            {
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
                        FieldName = "Publisher",
                        Values = new List<string>() { dto.Publisher },
                        Weight = _monitor.PublisherPercentage
                    }
                };
                // check if SubTitle is existed
                if (dto.SubTitle is not null)
                {
                    compareFields.First(cf => cf.FieldName.Equals("Title"))
                        .Values.Add(dto.SubTitle);
                }

                compareFields.Add(new FieldMatchInputDto()
                {
                    FieldName = "Authors",
                    Values = mainAuthor,
                    Weight = _monitor.AuthorNamePercentage
                });
                // check if GeneralNote is existed
                // if (dto.GeneralNote is not null)
                // {
                //     compareFields.Add(new FieldMatchInputDto()
                //     {
                //         FieldName = "Authors",
                //         Values = new List<string>() { dto.GeneralNote },
                //         Weight = _monitor.AuthorNamePercentage
                //     });
                // }

                var matchResult =
                    StringUtils.CalculateFieldMatchScore(processedOcrValue[i], compareFields,
                        _monitor.ConfidenceThreshold, _monitor.MinFieldThreshold);
                matchResult.ImageName = image.FileName;
                fieldMatchedResult.Add(i, matchResult);
            }

            StringComparision bestTitleMatched;
            StringComparision bestAuthorMatched;
            StringComparision bestPublisherMatched;


            // Get best matched title
            var titleMatchedEntries = fieldMatchedResult
                .Where(x => x.Value.FieldPointsWithThreshole
                    .Any(fp => fp.Name == "Title or Subtitle matches most"))
                .ToList();

            if (titleMatchedEntries.Any())
            {
                var entriesWithMaxPoints = titleMatchedEntries
                    .Select(x => new
                    {
                        Entry = x,
                        MaxPoint = x.Value.FieldPointsWithThreshole
                            .Where(fp => fp.Name == "Title or Subtitle matches most")
                            .Max(fp => Math.Max(fp.FuzzinessPoint, fp.MatchPhrasePoint))
                    })
                    .ToList();

                var overallMaxPoint = entriesWithMaxPoints.Max(x => x.MaxPoint);
                
                var bestMatchedWithTitleLine = entriesWithMaxPoints
                    .Where(x => x.MaxPoint == overallMaxPoint)
                    .Select(x => x.Entry)
                    .ToList();

                bestTitleMatched = new StringComparision
                {
                    FuzzinessPoint = bestMatchedWithTitleLine.Average(x =>
                        x.Value.FieldPointsWithThreshole
                            .Where(fp => fp.Name == "Title or Subtitle matches most")
                            .Select(fp => fp.FuzzinessPoint)
                            .First()),
                    MatchPhrasePoint = bestMatchedWithTitleLine.Average(x =>
                        x.Value.FieldPointsWithThreshole
                            .Where(fp => fp.Name == "Title or Subtitle matches most")
                            .Select(fp => fp.MatchPhrasePoint)
                            .First()),
                    FieldThreshold = double.Round(_monitor.TitlePercentage * 100),
                    PropertyName = "Title",
                };
                if(overallMaxPoint < _monitor.MinFieldThreshold)
                {
                    bestTitleMatched.MatchLine = "";
                }
                else
                {
                    bestTitleMatched.MatchLine = string.Join(" ", bestMatchedWithTitleLine.Select(x => processedOcrValue[x.Key]));
                }
            }
            else
            {
                bestTitleMatched = new StringComparision
                {
                    MatchLine = string.Empty,
                    FuzzinessPoint = 0,
                    MatchPhrasePoint = 0,
                    FieldThreshold = double.Round(_monitor.TitlePercentage * 100),
                    PropertyName = "Title",
                };
            }

            bestTitleMatched.MatchPercentage = bestTitleMatched.MatchPhrasePoint > bestTitleMatched.FuzzinessPoint
                ? bestTitleMatched.MatchPhrasePoint
                : bestTitleMatched.FuzzinessPoint;
            // Get best matched author
            var authorMatchedEntries = fieldMatchedResult
                .Where(x => x.Value.FieldPointsWithThreshole
                    .Any(fp => fp.Name.ToLower().Contains("author")))
                .ToList();

            if (authorMatchedEntries.Any())
            {
                var entriesWithMaxPoints = authorMatchedEntries
                    .Select(x => new
                    {
                        Entry = x,
                        MaxPoint = x.Value.FieldPointsWithThreshole
                            .Where(fp => fp.Name.ToLower().Contains("author"))
                            .Max(fp => Math.Max(fp.FuzzinessPoint, fp.MatchPhrasePoint))
                    })
                    .ToList();

                var overallMaxPoint = entriesWithMaxPoints.Max(x => x.MaxPoint);

                var bestMatchedWithAuthorLine = entriesWithMaxPoints
                    .Where(x => x.MaxPoint == overallMaxPoint)
                    .Select(x => x.Entry)
                    .ToList();

                bestAuthorMatched = new StringComparision
                {
                    FuzzinessPoint = bestMatchedWithAuthorLine.Average(x =>
                        x.Value.FieldPointsWithThreshole
                            .Where(fp => fp.Name.ToLower().Contains("author"))
                            .Select(fp => fp.FuzzinessPoint)
                            .First()),
                    MatchPhrasePoint = bestMatchedWithAuthorLine.Average(x =>
                        x.Value.FieldPointsWithThreshole
                            .Where(fp => fp.Name.ToLower().Contains("author"))
                            .Select(fp => fp.MatchPhrasePoint)
                            .First()),
                    FieldThreshold = double.Round(_monitor.AuthorNamePercentage * 100),
                    PropertyName = "Author"
                };
                if(overallMaxPoint < _monitor.MinFieldThreshold)
                {
                    bestAuthorMatched.MatchLine = "";
                }
                else
                {
                    bestAuthorMatched.MatchLine = string.Join(" ", bestMatchedWithAuthorLine.Select(x => processedOcrValue[x.Key]));
                }
            }
            else
            {
                bestAuthorMatched = new StringComparision
                {
                    MatchLine = string.Empty,
                    FuzzinessPoint = 0,
                    MatchPhrasePoint = 0,
                    FieldThreshold = double.Round(_monitor.AuthorNamePercentage * 100),
                    PropertyName = "Author"
                };
            }

            bestAuthorMatched.MatchPercentage = bestAuthorMatched.MatchPhrasePoint > bestAuthorMatched.FuzzinessPoint
                ? bestAuthorMatched.MatchPhrasePoint
                : bestAuthorMatched.FuzzinessPoint;

            // Get best matched publisher
            var publisherMatchedEntries = fieldMatchedResult
                .Where(x => x.Value.FieldPointsWithThreshole
                    .Any(fp => fp.Name.ToLower().Contains("publisher")))
                .ToList();

            if (publisherMatchedEntries.Any())
            {
                var entriesWithMaxPoints = publisherMatchedEntries
                    .Select(x => new
                    {
                        Entry = x,
                        MaxPoint = x.Value.FieldPointsWithThreshole
                            .Where(fp => fp.Name.ToLower().Contains("publisher"))
                            .Max(fp => Math.Max(fp.FuzzinessPoint, fp.MatchPhrasePoint))
                    })
                    .ToList();

                var overallMaxPoint = entriesWithMaxPoints.Max(x => x.MaxPoint);

                var bestMatchedWithPublisherLine = entriesWithMaxPoints
                    .Where(x => x.MaxPoint == overallMaxPoint)
                    .Select(x => x.Entry)
                    .ToList();

                bestPublisherMatched = new StringComparision
                {
                    FuzzinessPoint = bestMatchedWithPublisherLine.Average(x =>
                        x.Value.FieldPointsWithThreshole
                            .Where(fp => fp.Name.ToLower().Contains("publisher"))
                            .Select(fp => fp.FuzzinessPoint)
                            .First()),
                    MatchPhrasePoint = bestMatchedWithPublisherLine.Average(x =>
                        x.Value.FieldPointsWithThreshole
                            .Where(fp => fp.Name.ToLower().Contains("publisher"))
                            .Select(fp => fp.MatchPhrasePoint)
                            .First()),
                    FieldThreshold = double.Round(_monitor.PublisherPercentage * 100),
                    PropertyName = "Publisher"
                };
                if(overallMaxPoint < _monitor.MinFieldThreshold)
                {
                    bestPublisherMatched.MatchLine = "";
                }
                else
                {
                    bestPublisherMatched.MatchLine = string.Join(" ", bestMatchedWithPublisherLine.Select(x => processedOcrValue[x.Key]));
                }
            }
            else
            {
                bestPublisherMatched = new StringComparision
                {
                    MatchLine = string.Empty,
                    FuzzinessPoint = 0,
                    MatchPhrasePoint = 0,
                    FieldThreshold = double.Round(_monitor.PublisherPercentage * 100),
                    PropertyName = "Publisher"
                };
            }

            bestPublisherMatched.MatchPercentage =
                bestPublisherMatched.MatchPhrasePoint > bestPublisherMatched.FuzzinessPoint
                    ? bestPublisherMatched.MatchPhrasePoint
                    : bestPublisherMatched.FuzzinessPoint;
            var response = new PredictAnalysisDto()
            {
                StringComparisions = new List<StringComparision>(),
                LineStatisticDtos = new List<OcrLineStatisticDto>()
            };
            response.StringComparisions.Add(bestTitleMatched);
            response.StringComparisions.Add(bestAuthorMatched);
            response.StringComparisions.Add(bestPublisherMatched);

            foreach (var (key, value) in fieldMatchedResult)
            {
                var titleMatched =
                    value.FieldPointsWithThreshole.First(x => x.Name.Equals("Title or Subtitle matches most"));
                var authorMatched = value.FieldPointsWithThreshole.First(x => x.Name.ToLower().Contains("author"));
                var publisherMatched =
                    value.FieldPointsWithThreshole.First(x => x.Name.ToLower().Contains("publisher"));
                response.LineStatisticDtos.Add(new OcrLineStatisticDto()
                {
                    LineValue = processedOcrValue[key],
                    MatchedTitlePercentage = titleMatched.MatchPhrasePoint > titleMatched.FuzzinessPoint
                        ? titleMatched.MatchPhrasePoint
                        : titleMatched.FuzzinessPoint,
                    MatchedAuthorPercentage = authorMatched.MatchPhrasePoint > authorMatched.FuzzinessPoint
                        ? authorMatched.MatchPhrasePoint
                        : authorMatched.FuzzinessPoint,
                    MatchedPublisherPercentage = publisherMatched.MatchPhrasePoint > publisherMatched.FuzzinessPoint
                        ? publisherMatched.MatchPhrasePoint
                        : publisherMatched.FuzzinessPoint
                });
            }

            // use combination of every line to define the total matched point
            List<FieldMatchInputDto> combineCompareFields = new List<FieldMatchInputDto>()
            {
                new FieldMatchInputDto()
                {
                    FieldName = "Title",
                    Values = new List<string>() { dto.Title },
                    Weight = _monitor.TitlePercentage
                },
                new FieldMatchInputDto()
                {
                    FieldName = "Publisher",
                    Values = new List<string>() { dto.Publisher },
                    Weight = _monitor.PublisherPercentage
                }
            };
            // check if SubTitle is existed
            if (dto.SubTitle is not null)
            {
                combineCompareFields.First(cf => cf.FieldName.Equals("Title"))
                    .Values.Add(dto.SubTitle);
            }

            combineCompareFields.Add(new FieldMatchInputDto()
            {
                FieldName = "Authors",
                Values = mainAuthor,
                Weight = _monitor.AuthorNamePercentage
            });
            // check if GeneralNote is existed
            // if (dto.GeneralNote is not null)
            // {
            //     combineCompareFields.Add(new FieldMatchInputDto()
            //     {
            //         FieldName = "Authors",
            //         Values = new List<string>() { dto.GeneralNote },
            //         Weight = _monitor.AuthorNamePercentage
            //     });
            // }

            var combineMatchResult =
                StringUtils.CalculateFieldMatchScore(ocrValue, combineCompareFields,
                    _monitor.ConfidenceThreshold, _monitor.MinFieldThreshold);
            response.MatchPercentage = combineMatchResult.TotalPoint;
            response.OverallPercentage = _monitor.MinFieldThreshold;
            return new ServiceResult(ResultCodeConst.SYS_Success0002,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), response);
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process checking create book information");
        }
    }

    // public async Task<IServiceResult> CheckBookInformationAsync(CheckedBookEditionDto dto)
    // {
    //     try
    //     {
    //         //read information in book cover
    //         var result = await ReadFileStreamAsync(dto.Image.OpenReadStream());
    //         if (result.ResultCode != ResultCodeConst.SYS_Success0002)
    //         {
    //             return result;
    //         }
    //
    //         //prepare compare data
    //         List<FieldMatchInputDto> compareFields = new List<FieldMatchInputDto>()
    //         {
    //             new FieldMatchInputDto()
    //             {
    //                 FieldName = "Title",
    //                 Values = new List<string>() { dto.Title,dto.Subtitle },
    //                 Weight = _monitor.TitlePercentage
    //             },
    //             new FieldMatchInputDto()
    //             {
    //                 FieldName = "Authors",
    //                 Values = dto.Authors,
    //                 Weight = _monitor.AuthorNamePercentage
    //             },
    //             new FieldMatchInputDto()
    //             {
    //                 FieldName = "Publisher",
    //                 Values = new List<string>() { dto.Publisher },
    //                 Weight = _monitor.PublisherPercentage
    //             }
    //         };
    //         //compare data
    //         var matchResult =
    //             StringUtils.CalculateFieldMatchScore(result.Data.ToString(), compareFields,
    //                 _monitor.ConfidenceThreshold);
    //         if (matchResult.TotalPoint < matchResult.ConfidenceThreshold)
    //         {
    //             return new ServiceResult(ResultCodeConst.AIService_Warning0001,
    //                 await _msgService.GetMessageAsync(ResultCodeConst.AIService_Warning0001)
    //                 ,matchResult);
    //         }
    //         return new ServiceResult(ResultCodeConst.AIService_Success0001,
    //             await _msgService.GetMessageAsync(ResultCodeConst.AIService_Success0001)
    //             ,matchResult);
    //     }
    //     catch (Exception ex)
    //     {
    //         _logger.Error(ex.Message);
    //         throw new Exception("Error invoke when process checking create book information");
    //     }
    // }
    // public async Task<IServiceResult> CheckingImageWithImageDetail(CheckedBookEditionDto obj)
    // {
    //     try
    //     {
    //         // read information in book cover
    //         foreach (var objImage in obj.Images)
    //         {
    //             var result = await ReadFileStreamAsync(objImage.OpenReadStream());
    //             if (result.ResultCode != ResultCodeConst.SYS_Success0002)
    //             {
    //                 return result;
    //             }
    //             
    //         }
    //     }
    //     catch (Exception e)
    //     {
    //         Console.WriteLine(e);
    //         throw;
    //     }
    // }
//
//     public async Task<IServiceResult> CheckTrainingInputAsync(int bookEditionId, List<IFormFile> images)
//     {
//         try
//         {
//             var baseSpec = new BaseSpecification<BookEdition>(x => x.LibraryItemId == bookEditionId);
//             baseSpec.ApplyInclude(q => q.Include(x => x.BookEditionAuthors)
//                 .ThenInclude(ea => ea.Author));
//             //get book edition
//             var bookEditionResult = await _libraryItemService.GetWithSpecAsync(baseSpec);
//             if (bookEditionResult.ResultCode != ResultCodeConst.SYS_Success0002)
//             {
//                 return bookEditionResult;
//             }
//             var dto = (LibraryItemDto)bookEditionResult.Data!;
//                
//             var responseData = new TrainingImageMatchResultDto
//             {
//                 TrainingImageResult = new List<SingleImageMatchResultDto>()
//             };
//             foreach (var image in images)
//             {
//                 //read information in book cover
//                 var result = await ReadFileStreamAsync(image.OpenReadStream());
//                 if (result.ResultCode != ResultCodeConst.SYS_Success0002)
//                 {
//                     return result;
//                 }
//                 //prepare compare data
//                 var authors = dto.BookEditionAuthors.Where(x => x.BookEditionId == bookEditionId)
//                     .Select(x => x.Author.FullName).ToList();
//                 List<FieldMatchInputDto> compareFields = new List<FieldMatchInputDto>()
//                 {
//                     new FieldMatchInputDto()
//                     {
//                         FieldName = "Title",
//                         Values = new List<string>() { dto.EditionTitle },
//                         Weight = _monitor.TitlePercentage
//                     },
//                     new FieldMatchInputDto()
//                     {
//                         FieldName = "Authors",
//                         Values = authors,
//                         Weight = _monitor.AuthorNamePercentage
//                     },
//                     new FieldMatchInputDto()
//                     {
//                         FieldName = "Publisher",
//                         Values = new List<string>() { dto.Publisher },
//                         Weight = _monitor.PublisherPercentage
//                     }
//                 };
//                 //compare data
//                 var matchResult =
//                     StringUtils.CalculateFieldMatchScore(result.Data.ToString(), compareFields,
//                         _monitor.ConfidenceThreshold);
//                 responseData.TrainingImageResult.Add(new SingleImageMatchResultDto()
//                 {
//                     FieldPoints = matchResult.FieldPoints,
//                     ConfidenceThreshold = matchResult.ConfidenceThreshold,
//                     TotalPoint = matchResult.TotalPoint,
//                     ImageName = image.FileName,
//                 });
//             }
//
//             return new ServiceResult(ResultCodeConst.AIService_Success0001,
//                 await _msgService.GetMessageAsync(ResultCodeConst.AIService_Success0001)
//                 , responseData);
//         }
//         catch (Exception ex)
//         {
//             _logger.Error(ex.Message);
//             throw new Exception("Error invoke when process checking training book information");
//         }
//     }
}