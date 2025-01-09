using Nest;

namespace FPTU_ELibrary.Application.Utils;

public class ElasticUtils
{
    public static string? GetElasticFieldName<T>(string propertyName)
    {
        var property = typeof(T).GetProperty(propertyName);
        if (property == null) return null;
        
        // Check for Elasticsearch attribute and retrieve the 'Name' if defined
        var elasticAttribute = property.GetCustomAttributes(false)
            .OfType<ElasticsearchPropertyAttributeBase>()
            .FirstOrDefault();

        // Return defined 'Name'
        return elasticAttribute?.Name ?? property.Name; 
    }
}