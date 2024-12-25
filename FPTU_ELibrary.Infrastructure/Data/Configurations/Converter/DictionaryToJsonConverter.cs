using System.Text.Json;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace FPTU_ELibrary.Infrastructure.Data.Configurations.Converter;

//  Summary:
//      This class is to handle converting from dictionary to json and reverse 
public class DictionaryToJsonConverter : ValueConverter<Dictionary<string, object?>, string>
{
    // Inherited constructor
    public DictionaryToJsonConverter() 
        : base(
            // Convert from dictionary to json
            dictionary => JsonSerializer.Serialize(dictionary ?? new Dictionary<string, object?>(), 
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                }),
            // Convert from json to dictionary
            json => string.IsNullOrWhiteSpace(json)
                ? new Dictionary<string, object?>()
                : JsonSerializer.Deserialize<Dictionary<string, object?>>(json, 
                    new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
                    }) ?? new Dictionary<string, object?>())
    {
    }
}