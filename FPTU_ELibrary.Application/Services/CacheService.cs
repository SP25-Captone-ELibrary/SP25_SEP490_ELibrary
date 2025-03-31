using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Configurations;
using FPTU_ELibrary.Application.Dtos.LibraryItems;
using FPTU_ELibrary.Application.Extensions;
using FPTU_ELibrary.Application.Services.IServices;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Serilog;

namespace FPTU_ELibrary.Application.Services;

public class CacheService : ICacheService
{
    private readonly ILogger _logger;
    private readonly RedisSettings _redisSettings;
    private readonly IDistributedCache _cache;
    private readonly ISystemMessageService _msgService;
    
    private readonly ILibraryItemService<LibraryItemDto> _libItemSvc;

    public CacheService(
        IDistributedCache cache,
        ILibraryItemService<LibraryItemDto> libItemSvc,
        IOptionsMonitor<RedisSettings> monitor,
        ISystemMessageService msgService,
        ILogger logger)
    {
        _cache = cache;
        _logger = logger;
        _redisSettings = monitor.CurrentValue; 
        _msgService = msgService;
        _libItemSvc = libItemSvc;
    }

    public async Task<IServiceResult> GetOrAddLibraryItemForRecommendationAsync()
    {
        try
        {
            var cacheValue = await _cache.GetOrAddValueAsync<List<LibraryItemDto>, string>(
                anyKey: _redisSettings.LibraryItemCacheKey,
                factory: _ => _libItemSvc.GetAllForRecommendationAsync(),
                logger: _logger);
            
            // Mark as get data successfully
            return new ServiceResult(ResultCodeConst.SYS_Success0002,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), cacheValue.Value);
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process get or add library item for recommendation using cache");
        }
    }
    
    // public async Task<string> GetMessageAsync(string msgId)
    // {
    //     try
    //     {
    //         // Try to get system message from memory cache, create new if not exist
    //         var cacheValue = await _cache.GetOrAddValueAsync<SystemMessageDto, string>(
    //             msgId, _ => _msgService.GetByIdAsync(msgId), _logger);
    //
    //         // Retrieve global language
    //         var langStr = LanguageContext.CurrentLanguage;
    //         var langEnum = EnumExtensions.GetValueFromDescription<SystemLanguage>(langStr);
    //         // Define message Language
    //         var message = langEnum switch
    //         {
    //             SystemLanguage.Vietnamese => cacheValue.Value.Vi,
    //             SystemLanguage.English => cacheValue.Value.En,
    //             SystemLanguage.Russian => cacheValue.Value.Ru,
    //             SystemLanguage.Japanese => cacheValue.Value.Ja,
    //             SystemLanguage.Korean => cacheValue.Value.Ko,
    //             _ => cacheValue.Value.En
    //         };
    //         
    //         return message!;
    //     }
    //     catch (Exception)
    //     {
    //         throw;
    //     }
    // }
}