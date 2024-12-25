using System.ComponentModel;

namespace FPTU_ELibrary.Domain.Common.Enums;

public enum BookResourceType
{
    [Description("Sách điện tử")]
    Ebook,
    [Description("Sách nói")]
    AudioBook
}