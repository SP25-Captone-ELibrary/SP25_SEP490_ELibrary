using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Extensions;
using FPTU_ELibrary.Application.Services.IServices;
using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Interfaces;
using FPTU_ELibrary.Domain.Interfaces.Services;
using MapsterMapper;
using Microsoft.Extensions.Caching.Distributed;

namespace FPTU_ELibrary.Application.Services;

public class CacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private readonly ISystemMessageService _msgService;

    public CacheService(
        IDistributedCache cache,
        ISystemMessageService msgService)
    {
        _cache = cache;
        _msgService = msgService;
    }
    
    public async Task<string> GetMessageAsync(string msgId)
    {
        try
        {
            // Try to get system message from memory cache, create new if not exist
            var cacheValue = await _cache.GetOrAddValueAsync<SystemMessageDto, string>(
                msgId, _ => _msgService.GetByIdAsync(msgId));

            // Retrieve global language
            var langStr = LanguageContext.CurrentLanguage;
            var langEnum = EnumExtensions.GetValueFromDescription<SystemLanguage>(langStr);
            // Define message Language
            var message = langEnum switch
            {
                SystemLanguage.Vietnamese => cacheValue.Value.Vi,
                SystemLanguage.English => cacheValue.Value.En,
                SystemLanguage.Russian => cacheValue.Value.Ru,
                SystemLanguage.Japanese => cacheValue.Value.Ja,
                SystemLanguage.Korean => cacheValue.Value.Ko,
                _ => cacheValue.Value.En
            };
            
            return message!;
        }
        catch (Exception)
        {
            throw;
        }
    }
}