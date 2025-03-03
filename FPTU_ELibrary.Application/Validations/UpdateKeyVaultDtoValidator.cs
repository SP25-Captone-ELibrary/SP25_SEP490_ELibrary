using System.Reflection;
using FluentValidation;
using FPTU_ELibrary.Application.Dtos.AdminConfiguration;
using FPTU_ELibrary.Application.Extensions;
using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.Application.Validations;

public class UpdateKeyVaultDtoValidator:AbstractValidator<UpdateKeyVaultDto>
{
    private readonly Assembly _assembly;
    private readonly bool _isEnglish;
    private const string TargetNamespace = "FPTU_ELibrary.Application.Configurations";
    private const string assemblyName = "FPTU_ELibrary.Application";
    public UpdateKeyVaultDtoValidator(string langContext)
    {
        _assembly = LoadAssembly(assemblyName);

        var langEnum = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(langContext);
        _isEnglish = langEnum == SystemLanguage.English;

        RuleFor(x => x.FullFormatKey)
            .NotEmpty().WithMessage(GetMessage("FullFormatKey không được để trống.", "FullFormatKey cannot be empty."))
            .Must((dto, fullFormatKey) => ValidateClassAndProperty(fullFormatKey) == 0)
            .WithMessage(dto => GetValidationMessage(dto.FullFormatKey));

        RuleFor(x => x.Value)
            .NotEmpty().WithMessage(GetMessage("Value không được để trống", "Value cannot be empty"))
            .Must(BeParsableToPropertyType)
            .WithMessage(dto => GetMessage(
                "Vui lòng truyền đúng kiểu dữ liệu",
                "Please pass the correct data type"
            ));
    }

    private Assembly LoadAssembly(string assemblyName)
    {
        return Assembly.Load(assemblyName) ?? throw new Exception($"Cannot load assembly {assemblyName}");
    }

    /// <summary>
    /// check the existence of the class and property
    /// response: 0 = Available, 1 = Class not found, 2 = Property not found
    /// </summary>
    private int ValidateClassAndProperty(string fullFormatKey)
    {
        var parts = fullFormatKey.Split('-');
        if (parts.Length != 2) return 1;

        string className = parts[0];
        string propertyName = parts[1];

        Type classType = _assembly.GetTypes()
            .FirstOrDefault(t => t.IsClass && t.Namespace == TargetNamespace && t.Name == className);

        if (classType == null) return 1; 

        var property = classType.GetProperty(propertyName);
        if (property == null) return 2; 

        return 0; 
    }

    /// <summary>
    /// check if the value can be parsed to the property type
    /// </summary>
    private bool BeParsableToPropertyType(UpdateKeyVaultDto dto, string value)
    {
        var parts = dto.FullFormatKey.Split('-');
        if (parts.Length != 2) return false;

        string className = parts[0];
        string propertyName = parts[1];

        Type classType = _assembly.GetTypes()
            .FirstOrDefault(t => t.IsClass && t.Namespace == TargetNamespace && t.Name == className);

        if (classType == null) return false; 

        var property = classType.GetProperty(propertyName);
        if (property == null) return false; 

        Type propertyType = property.PropertyType;
        
        try
        {
            if (propertyType == typeof(int)) int.Parse(value);
            else if (propertyType == typeof(double)) double.Parse(value);
            else if (propertyType == typeof(bool)) bool.Parse(value);
            else if (propertyType == typeof(DateTime)) DateTime.Parse(value);
            else if (propertyType == typeof(Guid)) Guid.Parse(value);
            else return propertyType == typeof(string);

            return true;
        }
        catch
        {
            return false; 
        }
    }

    /// <summary>
    /// get message by language
    /// </summary>
    private string GetMessage(string messageVi, string messageEn)
    {
        return _isEnglish ? messageEn : messageVi;
    }

    /// <summary>
    /// create message for validation class and property
    /// </summary>
    private string GetValidationMessage(string fullFormatKey)
    {
        int validationResult = ValidateClassAndProperty(fullFormatKey);
        switch (validationResult)
        {
            case 1:
                return GetMessage($"Không tìm thấy class: {fullFormatKey.Split('-')[0]}",
                                  $"Class not found: {fullFormatKey.Split('-')[0]}");
            case 2:
                return GetMessage($"Không tìm thấy property: {fullFormatKey}",
                                  $"Property not found: {fullFormatKey}");
            default:
                return "";
        }
    }
}
