using System.Reflection;
using Azure.Data.AppConfiguration;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Configurations;
using FPTU_ELibrary.Application.Dtos.AdminConfiguration;
using FPTU_ELibrary.Application.Exceptions;
using FPTU_ELibrary.Application.Extensions;
using FPTU_ELibrary.Application.Services.IServices;
using FPTU_ELibrary.Application.Utils;
using FPTU_ELibrary.Application.Validations;
using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Serilog;
using ConfigurativeObjectDetail = FPTU_ELibrary.Application.Dtos.AdminConfiguration.ConfigurativeObjectDetail;

namespace FPTU_ELibrary.Application.Services;

public class AdminConfigurationService : IAdminConfigurationService
{
    private readonly ISystemMessageService _msgService;
    private readonly ILogger _logger;
    private readonly AzureSettings _monitor;
    private readonly SecretClient _client;
    private const string SRC = "FPTU_ELibrary.Application";
    private const string SETTINGS_FOLDER = "FPTU_ELibrary.Application.Configurations";
    private readonly ConfigurationClient _newClient;

    public AdminConfigurationService(IOptionsMonitor<AzureSettings> monitor,
        ISystemMessageService msgService,
        IConfiguration configuration,
        ILogger logger)
    {
        var connectionString = configuration.GetConnectionString("AzureAppConfiguration");
        _newClient = new ConfigurationClient(connectionString);
        _msgService = msgService;
        _logger = logger;
        _monitor = monitor.CurrentValue;
        var keyVaultUrl = _monitor.KeyVaultUrl;
        var clientId = _monitor.KeyVaultClientId;
        var clientSecret = _monitor.KeyVaultClientSecret;
        var tenantId = _monitor.KeyVaultDirectoryID;
        var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
        _client = new SecretClient(new Uri(keyVaultUrl), credential);
    }

    public async Task<IServiceResult> GetAllKeyVault()
    {
        var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
            LanguageContext.CurrentLanguage);
        var isEng = lang == SystemLanguage.English;

        var secrets = _client.GetPropertiesOfSecrets().ToList();
        var processedPrefixes = new HashSet<string>();
        List<ConfigurativeObject> result = new();

        foreach (var secretProperty in secrets)
        {
            var secretName = secretProperty.Name;
            if (secretName is null)
            {
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(errMsg, isEng
                    ? "Secret name is null"
                    : "Tên secret không tồn tại");
            }

            var prefix = StringUtils.DecodeSecretPrefix(secretName);
            if (processedPrefixes.Contains(prefix))
            {
                continue;
            }

            processedPrefixes.Add(prefix);

            var samePrefix = secrets.Where(sp => sp.Name.StartsWith($"{prefix}-")).ToList();
            ConfigurativeObject obj = new ConfigurativeObject()
            {
                Name = prefix,
                ObjectKeyValuePairs = new List<ObjectKeyValuePair>()
            };

            foreach (var sp in samePrefix)
            {
                string key = sp.Name.Remove(0, prefix.Length + 1);

                // Get the exact type of the property
                Type? propertyType = GetPropertyTypeFromKey(prefix, key);
                int propertyDataType = (int)StringUtils.GetPropertyDataType(propertyType);

                obj.ObjectKeyValuePairs.Add(new ObjectKeyValuePair()
                {
                    Key = key,
                    Value = _client.GetSecret(sp.Name).Value.Value,
                    Type = propertyDataType
                });
            }

            result.Add(obj);
        }

        return new ServiceResult(ResultCodeConst.SYS_Success0002,
            await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), result);
    }


    public async Task<IServiceResult> GetKeyVault(string key)
    {
        var secret = await _client.GetSecretAsync(key);
        if (secret.Value is null)
        {
            var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
            return new ServiceResult(errMsg, "Secret value is null");
        }

        var prefix = StringUtils.DecodeSecretPrefix(key);
        string propertyKey = key.Remove(0, prefix.Length + 1);

        // Lấy Type của property
        Type? propertyType = GetPropertyTypeFromKey(prefix, propertyKey);
        int propertyDataType = (int)StringUtils.GetPropertyDataType(propertyType);

        var result = new ConfigurativeObjectDetail()
        {
            Name = prefix,
            Key = propertyKey,
            Value = secret.Value.Value,
            Type = propertyDataType
        };

        return new ServiceResult(ResultCodeConst.SYS_Success0002,
            await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), result);
    }


    public async Task<IServiceResult> UpdateKeyVault(IDictionary<string, string> keyValues)
    {
        foreach (var keyValuePair in keyValues)
        {
            // Create FullFormatKey to check validation
            UpdateKeyVaultDto req = new UpdateKeyVaultDto()
            {
                FullFormatKey = keyValuePair.Key,
                Value = keyValuePair.Value
            };
            // Validate inputs using the generic validator
            var validationResult = await ValidatorExtensions.ValidateAsync(req);
            if (validationResult != null && !validationResult.IsValid)
            {
                // Convert ValidationResult to ValidationProblemsDetails.Errors
                var errors = validationResult.ToProblemDetails().Errors;
                throw new UnprocessableEntityException("Invalid Validations", errors);
            }

            var secret = await _client.GetSecretAsync(keyValuePair.Key);
            if (secret.Value is null)
            {
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(errMsg, "Secret value is null");
            }

            var secretProperties = new KeyVaultSecret(keyValuePair.Key, keyValuePair.Value);
            await _client.SetSecretAsync(secretProperties);
        }

        return new ServiceResult(ResultCodeConst.SYS_Success0003,
            await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003));
    }

    public async Task<IServiceResult> GetAllInAzureConfiguration()
    {
        var settings = new Dictionary<string, object>();  // Use object to store deserialized data.

        await foreach (ConfigurationSetting setting in _newClient.GetConfigurationSettingsAsync(new SettingSelector { }))
        {
            if (string.IsNullOrEmpty(setting.Key) || string.IsNullOrEmpty(setting.Value)) continue;

            var parts = setting.Key.Split(":", 2);
            if (parts.Length != 2) continue;

            var section = parts[0];
            var key = parts[1];

            if (!settings.ContainsKey(section))
                settings[section] = new Dictionary<string, object>();

            // Deserialize JSON values if they are related to "LibrarySchedule"
            if (section == "AppSettings" && key == "LibrarySchedule")
            {
                try
                {
                    var librarySchedule = JsonConvert.DeserializeObject<LibrarySchedule>(setting.Value);
                    (settings[section] as Dictionary<string, object>)![key] = librarySchedule;
                }
                catch (JsonException ex)
                {
                    // Handle any deserialization error (optional)
                    Console.WriteLine($"Error deserializing LibrarySchedule: {ex.Message}");
                }
            }
            else
            {
                (settings[section] as Dictionary<string, object>)![key] = setting.Value;
            }
        }

        return new ServiceResult(ResultCodeConst.SYS_Success0002,
            await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), settings);
    }

    public async Task<IServiceResult> UpdateKeyValueAzureConfiguration(string name, string value)
    {
        UpdateKeyVaultDto dto = new UpdateKeyVaultDto
        {
            FullFormatKey = name,
            Value = value
        };

        // Validate inputs using the generic validator
        var validationResult = await ValidatorExtensions.ValidateAsync(dto);
        if (validationResult != null && !validationResult.IsValid)
        {
            // Convert ValidationResult to ValidationProblemsDetails.Errors
            var errors = validationResult.ToProblemDetails().Errors;
            throw new UnprocessableEntityException("Invalid Validations", errors);
        }

        var existing = await _newClient.GetConfigurationSettingAsync(name);
        var updated = new ConfigurationSetting(name, value);
        await _newClient.SetConfigurationSettingAsync(updated);
        Random random = new Random();
        var updateResetValue = new ConfigurationSetting("AppSettings:RefreshValue", random.Next().ToString());
        await _newClient.SetConfigurationSettingAsync(updateResetValue);

        return new ServiceResult(ResultCodeConst.SYS_Success0003,
            await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003), true);
    }
    public async Task<IServiceResult> UpdateLibraryScheduleAsync(List<WorkDateAndTime> updates)
    {
        // B1: Lấy setting hiện tại
        var setting = await _newClient.GetConfigurationSettingAsync("AppSettings:LibrarySchedule");
        var json = setting?.Value?.Value;

        var current = string.IsNullOrWhiteSpace(json)
            ? new PerformedLibrarySchedule()
            : JsonConvert.DeserializeObject<PerformedLibrarySchedule>(json);
        var validationResult = await ValidatorExtensions.ValidateAsync(current.ToLibrarySchedule());
        if (validationResult != null && !validationResult.IsValid)
        {
            // Convert ValidationResult to ValidationProblemsDetails.Errors
            var errors = validationResult.ToProblemDetails().Errors;
            throw new UnprocessableEntityException("Invalid Validations", errors);
        }

        // B2: Loại bỏ các ngày cũ đã có trong input
        var updateDays = updates.Select(x => x.WeekDate).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var schedule in current.Schedules.ToList())
        {
            schedule.Days.RemoveAll(day => updateDays.Contains(day));

            if (!schedule.Days.Any())
            {
                current.Schedules.Remove(schedule);
            }
        }

        // B3: Thêm các update mới vào
        foreach (var update in updates)
        {
            current.Schedules.Add(new Schedule
            {
                Days = new List<string> { update.WeekDate },
                Open = TimeSpan.Parse(update.Open).ToString(@"hh\:mm\:ss"),
                Close = TimeSpan.Parse(update.Close).ToString(@"hh\:mm\:ss")
            });
        }

        // B4: Gom nhóm lại toàn bộ Schedules
        var groupedSchedules = current.Schedules
            .SelectMany(s => s.Days.Select(day => new { Day = day, Open = s.Open, Close = s.Close }))
            .GroupBy(x => new { x.Open, x.Close })
            .Select(g => new Schedule
            {
                Open = g.Key.Open,
                Close = g.Key.Close,
                Days = g.Select(x => x.Day).OrderBy(d => GetDayOfWeekValue(d)).ToList() // sắp xếp cho đẹp
            })
            .ToList();

        current.Schedules = groupedSchedules;

        // B5: Validate
        

        // B6: Ghi lại
        var newJson = JsonConvert.SerializeObject(current, Formatting.Indented);
        await _newClient.SetConfigurationSettingAsync("AppSettings:LibrarySchedule", newJson);
        return new ServiceResult(ResultCodeConst.SYS_Success0003,
            await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003));
    }
    private static int GetDayOfWeekValue(string day)
    {
        // đảm bảo đúng thứ tự Monday -> Sunday
        return day switch
        {
            "Monday" => 1,
            "Tuesday" => 2,
            "Wednesday" => 3,
            "Thursday" => 4,
            "Friday" => 5,
            "Saturday" => 6,
            "Sunday" => 0,
            _ => -1
        };
    }

    public static LibrarySchedule UpdateLibrarySchedule(
        List<WorkDateAndTime> userInput,
        LibrarySchedule originalSchedule)
    {
        var result = new LibrarySchedule();

        var grouped = userInput
            .GroupBy(x => new { x.Open, x.Close })
            .Select(g => new DaySchedule
            {
                Days = g.Select(x => Enum.Parse<DayOfWeek>(x.WeekDate)).ToList(),
                Open = TimeSpan.Parse(g.Key.Open),
                Close = TimeSpan.Parse(g.Key.Close)
            });

        result.Schedules.AddRange(grouped);
        return result;
    }

    private Type? GetPropertyTypeFromKey(string className, string propertyName)
    {
        // Load the target assembly
        Assembly assembly = Assembly.Load(SRC);

        // Find the class by name inside the namespace
        Type? classType = assembly.GetTypes()
            .FirstOrDefault(t => t.IsClass && t.Namespace == SETTINGS_FOLDER && t.Name == className);

        if (classType == null) return null;

        // Find the property inside the class
        PropertyInfo? property = classType.GetProperty(propertyName);
        return property?.PropertyType; // Return the exact type
    }
}