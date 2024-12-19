using System.Text.RegularExpressions;
using CsvHelper.Configuration.Attributes;

namespace FPTU_ELibrary.Application.Dtos.Authors;

public class AuthorCsvRecord
{
    [Name("AuthorCode")]
    public string? AuthorCode { get; set; }
    
    [Name("FullName")]
    public string FullName { get; set; } = null!;
    
    [Name("Biography")]
    public string? Biography { get; set; }
    
    [Name("Dob")]
    public string Dob { get; set; } = null!;
    
    [Name("DateOfDeath")]
    public string? DateOfDeath { get; set; }
    
    [Name("Nationality")]
    public string Nationality { get; set; } = null!;
}

public static class AuthorCsvRecordExtensions
{
    public static List<AuthorDto> ToAuthorDtosForImport(this IEnumerable<AuthorCsvRecord> records)
    {
        // Current local datetime
        var currentLocalDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
            // Vietnam timezone
            TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));

        return records.Select(rc => new AuthorDto()
        {
            AuthorCode = rc.AuthorCode,
            FullName = rc.FullName,
            Biography = rc.Biography,
            Dob = !string.IsNullOrEmpty(rc.Dob) 
                ? DateTime.Parse(rc.Dob) : null,
            DateOfDeath = !string.IsNullOrEmpty(rc.DateOfDeath) 
                ? DateTime.Parse(rc.DateOfDeath) : null,
            CreateDate = currentLocalDateTime,
            IsDeleted = false
        }).ToList();
    }

    public static List<AuthorCsvRecord> ToAuthorCsvRecords(this IEnumerable<AuthorDto> dtos)
    {
        return dtos.Select(u => new AuthorCsvRecord
        {
            AuthorCode = u.AuthorCode,
            FullName = u.FullName,
            Biography = Regex.Replace(u.Biography ?? string.Empty, "<.*?>", string.Empty),
            Dob = u.Dob.HasValue ? u.Dob.Value.ToString("yyyy-MM-dd") : string.Empty,
            DateOfDeath = u.DateOfDeath.HasValue ? u.DateOfDeath.Value.ToString("yyyy-MM-dd") : string.Empty,
            Nationality = u.Nationality ?? string.Empty
        }).ToList();
    }
}