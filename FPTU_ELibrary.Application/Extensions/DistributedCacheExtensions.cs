using FPTU_ELibrary.Application.Dtos.Cache;
using FPTU_ELibrary.Application.Exceptions;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using Serilog;
using StackExchange.Redis;

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
        Func<TKey, Task<IServiceResult>> factory,
        ILogger logger
    ) 
        where T : class
    {
        // Convert any type of key to string
        var key = ConvertKeyToString(anyKey);

        try
        {
            // Try to get the value from the cache
            var value = await cache.SafeGetAsync<T>(key, logger);
            if (value == null)
            {
                // Fetch data from the database or other source
                var result = await factory(anyKey);
                value = result.Data as T;

                if (value == null) // Data not found
                    throw new NotFoundException("Not found data with key: " + key);

                // Cache the data for future use
                await cache.SafeSetStringAsync(key, JsonConvert.SerializeObject(value));

                // Return the newly fetched value
                return new(false, value);
            }

            // Return cached value
            return new(true, value);
        }
        catch (Exception ex)
        {
            logger.Error("Error accessing distributed cache for key {key}: {ex}", key, ex.Message);
        }

        return null!;
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
    /// Safe wrapper for GetStringAsync
    /// </summary>
    private static async Task<string?> SafeGetStringAsync(this IDistributedCache cache, string key, ILogger logger)
    {
        try
        {
            return await cache.GetStringAsync(key);
        }
        catch (Exception ex)
        {
            // Log the cache access failure
            logger.Error("Cache access failed for key {key}: {msg}", key, ex.Message);
            return null; // Gracefully fall back
        }
    }
    
    /// <summary>
    /// Safe wrapper for GetAsync
    /// </summary>
    private static async Task<T?> SafeGetAsync<T>(this IDistributedCache cache, string key, ILogger logger)
        where T : class
    {
        var jsonValue = await cache.SafeGetStringAsync(key, logger);
        return string.IsNullOrEmpty(jsonValue) ? null : JsonConvert.DeserializeObject<T>(jsonValue);
    }
    
    /// <summary>
    /// Safe wrapper for SetStringAsync
    /// </summary>
    private static async Task SafeSetStringAsync(this IDistributedCache cache, string key, string value)
    {
        try
        {
            await cache.SetStringAsync(key, value);
        }
        catch (Exception ex)
        {
            // Log the failure to set the cache
            Console.Error.WriteLine($"Failed to set cache for key {key}: {ex.Message}");
        }
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
    public static async Task SetAsync<TKey, TValue>(this IDistributedCache cache,
        TKey anyKey, TValue value, ILogger logger
    )
        where TValue : class
    {
        try
        {
            var key = ConvertKeyToString(anyKey);
            await cache.SetStringAsync(key, JsonConvert.SerializeObject(value));
        }
        catch (RedisConnectionException)
        {
            logger.Error("Failed to connect with memory cache server, please re-try");
        }
        catch (Exception ex)
        {
            logger.Error("Failed to set cache for key {anyKey}: {msg}", anyKey, ex.Message);
        }
    }
}