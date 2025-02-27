using System.ComponentModel;

namespace FPTU_ELibrary.Domain.Common.Enums;

public enum TransactionType
{
    [Description("Phí phạt")]
    Fine,
    [Description("Mượn tài liệu điện tử")]
    DigitalBorrow,
    [Description("Đăng ký thẻ thư viện")]
    LibraryCardRegister,
    [Description("Gia hạn thẻ thư viện")]
    LibraryCardExtension,
    [Description("Gia hạn tài liệu điện tử")]
    DigitalExtension,
    
    // Add more type... 
}