using System.Text.RegularExpressions;
using CsvHelper.Configuration.Attributes;
using FPTU_ELibrary.Application.Configurations;
using FPTU_ELibrary.Application.Dtos.LibraryItems;
using FPTU_ELibrary.Application.Extensions;
using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces.Repositories.Base;
using OpenCvSharp.XPhoto;
using Org.BouncyCastle.Ocsp;

namespace FPTU_ELibrary.Application.Dtos.LibraryCard;

public class LibraryCardHolderCsvRecordDto
{
    [Name("Email")] 
    public string Email { get; set; } = null!;
    
    [Name("Họ")] 
    public string FirstName { get; set; } = null!;
    
    [Name("Tên")] 
    public string LastName { get; set; } = null!;
    
    [Name("SĐT")]
    public string? Phone { get; set; } 
    
    [Name("Địa chỉ")]
    public string? Address { get; set; } 
    
    [Name("Giới tính")]
    public string? Gender { get; set; } = null!;
    
    [Name("Ngày sinh")]
    public DateTime? Dob { get; set; }

    // Library card information (if any)
    [Name("Tên thẻ thư viện")]
    public string? LibraryCardFullName { get; set; } = null!;
    
    [Name("Ảnh thẻ")]
    public string? LibraryCardAvatar { get; set; }

    [Name("Mã thẻ")]
    public string? Barcode { get; set; }
    
    [Name("Hình thức đăng ký")] 
    public string? IssuanceMethod { get; set; }
    
    [Name("Trạng thái thẻ")] 
    public string? LibraryCardStatus { get; set; }
    
    [Name("Ngày tạo thẻ")] 
    public DateTime? IssueDate { get; set; }
    
    [Name("Ngày hết hạn")]
    public DateTime? ExpiryDate { get; set; }
    
    [Name("Tạo thẻ thư viện")] 
    public bool IsCreateLibraryCard { get; set; }
}

public static class LibraryCardHolderCsvRecordDtoExtensions
{
    public static UserDto ToUserDto(
        this LibraryCardHolderCsvRecordDto record,
        int roleId,
        BorrowSettings borrowSettings,
        string? libraryCardAvatar = null)
    {
        // Current local datetime
        var currentLocalDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
            // Vietnam timezone
            TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
        
        return new()
        {
            Email = record.Email,
            FirstName = record.FirstName,
            LastName = record.LastName,
            Phone = record.Phone,
            Gender = record.Gender,
            Address = record.Address,
            Dob = record.Dob,
            CreateDate = currentLocalDateTime,
            RoleId = roleId,
            LibraryCard = record.IsCreateLibraryCard
                ? new()
                {
                    FullName = record.LibraryCardFullName ?? string.Empty,
                    Avatar = libraryCardAvatar ?? string.Empty,
                    Barcode = record.Barcode ?? string.Empty,
                    IssuanceMethod = (LibraryCardIssuanceMethod) Enum.Parse(typeof(LibraryCardIssuanceMethod), 
                        record.IssuanceMethod ?? string.Empty, true),
                    Status = (LibraryCardStatus) Enum.Parse(typeof(LibraryCardStatus), 
                        record.LibraryCardStatus ?? string.Empty, true),
                    IssueDate = record.IssueDate ?? currentLocalDateTime,
                    ExpiryDate = record.ExpiryDate ?? record.IssueDate!.Value.AddDays(borrowSettings.PickUpExpirationInDays)
                }
                : null
        };
    }

    public static LibraryCardHolderCsvRecordDto ToCardHolderCsvRecordDto(this UserDto dto)
    {
        return new()
        {
            Email = dto.Email,
            FirstName = dto.FirstName ?? string.Empty,
            LastName = dto.LastName ?? string.Empty,
            Phone = dto.Phone ?? string.Empty,
            Gender = dto.Gender,
            Address = dto.Address,
            Dob = dto.Dob,
            LibraryCardFullName = dto.LibraryCard != null ? dto.LibraryCard.FullName : string.Empty,
            LibraryCardAvatar = dto.LibraryCard != null ? dto.LibraryCard.Avatar : string.Empty,
            LibraryCardStatus = dto.LibraryCard != null ? dto.LibraryCard.Status.ToString() : string.Empty,
            IssuanceMethod = dto.LibraryCard != null ? dto.LibraryCard.IssuanceMethod.ToString() : string.Empty,
            Barcode = dto.LibraryCard != null ? dto.LibraryCard.Barcode : string.Empty,
            IssueDate = dto.LibraryCard?.IssueDate,
            ExpiryDate = dto.LibraryCard?.ExpiryDate
        };
    }
}
