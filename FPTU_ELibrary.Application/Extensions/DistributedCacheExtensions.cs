using FPTU_ELibrary.Application.Dtos.Cache;
using FPTU_ELibrary.Application.Exceptions;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;

namespace FPTU_ELibrary.Application.Extensions;

//  Summary:
//      This class provides extension methods for IDistributedCache
//      in order to handle caching for the application. 
public static class DistributedCacheExtensions
{
    /// <summary>
    /// Get the value from the cache and add new when it not exist
    /// </summary>
    /// <param name="cache"></param>
    /// <param name="anyKey"></param>
    /// <param name="factory"></param>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TKey"></typeparam>
    /// <returns></returns>
    public static async Task<GetCachedValueDto<T>> GetOrAddValueAsync<T, TKey>(
        this IDistributedCache cache,
        TKey anyKey,
        Func<TKey, Task<IServiceResult>> factory
    ) 
        where T : class
    {
        try
        {
            var key = ConvertKeyToString(anyKey);
        
            // Get value by key
            var value = await cache.GetAsync<T>(key);
            if (value == null)
            {
                // Get service result data, convert to generic type
                value = (await factory(anyKey)).Data as T;
                if (value == null) // Not exist 
                    throw new NotFoundException("Error when add memory cache data:", "Key " + key);
                
                // Add JSON data to cache memory
                await cache.SetStringAsync(key, JsonConvert.SerializeObject(value));
                
                // Return value and mark as not cached 
                return new(false, value);
            }

            // Return value and mark as cached 
            return new(true, value);
        }
        catch (Exception)
        {
            throw;
        }
    }

    /// <summary>
    /// Convert any key type to string
    /// </summary>
    /// <param name="anyKey"></param>
    /// <typeparam name="TKey"></typeparam>
    /// <returns></returns>
    private static string ConvertKeyToString<TKey>(TKey anyKey)
    {
        // Convert TKey into string
        var key = anyKey switch
        {
            // Default as string 
            string k => k,
            // Other type to string
            _ => (anyKey?.ToString())!
        };

        return key;
    }
    
    /// <summary>
    /// Get value by key
    /// </summary>
    /// <param name="cache"></param>
    /// <param name="key"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    private static async Task<T?> GetAsync<T>(this IDistributedCache cache, string key)
        where T : class
    {
        // Get value string
        var jsonValue = await cache.GetStringAsync(key);
        if (string.IsNullOrEmpty(jsonValue)) // Not exist
        {
            return null;
        }

        // Convert string value to generic object 
        return JsonConvert.DeserializeObject<T>(jsonValue);
    }
    
    /// <summary>
    /// Get long value by key
    /// </summary>
    /// <param name="cache"></param>
    /// <param name="key"></param>
    /// <returns></returns>
    public static async Task<long> GetLongAsync(this IDistributedCache cache, string key)
    {
        var value = await cache.GetAsync(key);
        return BitConverter.ToInt64(value);
    }

    /// <summary>
    /// Set long value
    /// </summary>
    /// <param name="cache"></param>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public static Task SetLongAsync(this IDistributedCache cache, string key, long value)
    {
        return cache.SetAsync(key, BitConverter.GetBytes(value));
    }
    
    /// <summary>
    /// Get datetime by key
    /// </summary>
    /// <param name="cache"></param>
    /// <param name="key"></param>
    /// <returns></returns>
    public static async Task<DateTime> GetDateTimeAsync(this IDistributedCache cache, string key)
    {
        var value = await cache.GetAsync(key);
        var ticks = BitConverter.ToInt64(value);
        return new(ticks);
    }

    /// <summary>
    /// Set datetime value
    /// </summary>
    /// <param name="cache"></param>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public static Task SetDateTimeAsync(this IDistributedCache cache, string key, DateTime value)
    {
        var ticks = value.Ticks;
        return cache.SetAsync(key, BitConverter.GetBytes(ticks));
    }

    /// <summary>
    /// Set value directly to cache memory
    /// </summary>
    /// <param name="cache"></param>
    /// <param name="anyKey"></param>
    /// <param name="value"></param>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    /// <returns></returns>
    public static Task SetAsync<TKey, TValue>(this IDistributedCache cache, TKey anyKey, TValue value)
        where TValue : class
    {
        var key = ConvertKeyToString(anyKey);
        return cache.SetStringAsync(key, JsonConvert.SerializeObject(value));
    }
}