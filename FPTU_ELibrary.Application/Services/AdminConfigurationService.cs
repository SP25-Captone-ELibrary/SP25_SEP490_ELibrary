using System.Reflection;
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
using Microsoft.Extensions.Options;
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

    public AdminConfigurationService(IOptionsMonitor<AzureSettings> monitor,
        ISystemMessageService msgService,
        ILogger logger)
    {
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


    public async Task<IServiceResult> UpdateKeyVault(string key, string value)
    {
        // Create FullFormatKey to check validation
        UpdateKeyVaultDto req = new UpdateKeyVaultDto()
        {
            FullFormatKey = key,
            Value = value
        };
        // Validate inputs using the generic validator
        var validationResult = await ValidatorExtensions.ValidateAsync(req);
        if (validationResult != null && !validationResult.IsValid)
        {
            // Convert ValidationResult to ValidationProblemsDetails.Errors
            var errors = validationResult.ToProblemDetails().Errors;
            throw new UnprocessableEntityException("Invalid Validations", errors);
        }

        var secret = await _client.GetSecretAsync(key);
        if (secret.Value is null)
        {
            var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
            return new ServiceResult(errMsg, "Secret value is null");
        }

        var secretProperties = new KeyVaultSecret(key, value);
        await _client.SetSecretAsync(secretProperties);
        return new ServiceResult(ResultCodeConst.SYS_Success0003,
            await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003));
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