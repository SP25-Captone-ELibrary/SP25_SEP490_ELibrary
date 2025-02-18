using System.ComponentModel;

namespace FPTU_ELibrary.Domain.Common.Enums;

public enum LibraryCardIssuanceMethod
{
    [Description("Tại thư viện")]
    InPerson,
    [Description("Trực tuyến")]
    Online
}