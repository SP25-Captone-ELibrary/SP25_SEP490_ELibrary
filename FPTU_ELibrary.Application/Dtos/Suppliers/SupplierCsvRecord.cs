using CsvHelper.Configuration.Attributes;
using FPTU_ELibrary.Application.Dtos.LibraryItems;
using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Entities;

namespace FPTU_ELibrary.Application.Dtos.Suppliers;

public class SupplierCsvRecord
{
    [Name("SupplierName")]
    public string SupplierName { get; set; } = null!;
    
    [Name("SupplierType")]
    public string SupplierType { get; set; } = null!;
    
    [Name("ContactPerson")]
    public string? ContactPerson { get; set; }
    
    [Name("ContactEmail")]
    public string? ContactEmail { get; set; }
    
    [Name("ContactPhone")]
    public string? ContactPhone { get; set; }
    
    [Name("Address")]
    public string? Address { get; set; }
    
    [Name("Country")]
    public string? Country { get; set; }
    
    [Name("City")]
    public string? City { get; set; }
}

public static class SupplierCsvRecordExtensions
{
    public static SupplierDto ToSupplierDto(this SupplierCsvRecord record)
    {
        return new()
        {
            SupplierName = record.SupplierName,
            SupplierType = Enum.TryParse(record.SupplierType, true, out SupplierType supplierType) 
                ? supplierType : SupplierType.Publisher,
            ContactPerson = record.ContactPerson,
            ContactEmail = record.ContactEmail,
            ContactPhone = record.ContactPhone,
            Address = record.Address,
            Country = record.Country,
            City = record.City
        };
    }
    
    public static SupplierCsvRecord ToSupplierCsvRecord(this SupplierDto dto)
    {
        return new()
        {
            SupplierName = dto.SupplierName,
            SupplierType = dto.SupplierType.ToString(),
            ContactPerson = dto.ContactPerson,
            ContactEmail = dto.ContactEmail,
            ContactPhone = dto.ContactPhone,
            Address = dto.Address,
            Country = dto.Country,
            City = dto.City,
        };
    }
}