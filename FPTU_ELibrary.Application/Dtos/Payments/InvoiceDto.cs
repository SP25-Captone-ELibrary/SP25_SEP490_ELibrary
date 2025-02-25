using System.Text.Json.Serialization;
using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.Application.Dtos.Payments;

public class InvoiceDto
{
    public int InvoiceId { get; set; }
    public string InvoiceCode { get; set; }
    public Guid UserId { get; set; }
    public int PaymentMethodId { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? PaidAt { get; set; }

    public UserDto User { get; set; } = null!;
    
    [JsonIgnore]
    public ICollection<TransactionDto> Transactions { get; set; } = new List<TransactionDto>();
    public PaymentMethodDto PaymentMethod { get; set; } = null!;
}