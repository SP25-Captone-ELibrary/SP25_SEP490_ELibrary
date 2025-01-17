using System.Text.Json.Serialization;

namespace FPTU_ELibrary.Application.Dtos.Payments;

public class PaymentMethodDto
{
    public int PaymentMethodId { get; set; }
    public string MethodName { get; set; } = null!;

    [JsonIgnore]
    public ICollection<TransactionDto> Transactions { get; set; } = new List<TransactionDto>();
}