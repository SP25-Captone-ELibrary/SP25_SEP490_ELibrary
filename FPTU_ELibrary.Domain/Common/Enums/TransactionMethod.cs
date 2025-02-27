using System.ComponentModel;

namespace FPTU_ELibrary.Domain.Common.Enums;

public enum TransactionMethod
{
    [Description("Tiền mặt")]
    Cash,
    [Description("Thanh toán điện tử")]
    DigitalPayment
}