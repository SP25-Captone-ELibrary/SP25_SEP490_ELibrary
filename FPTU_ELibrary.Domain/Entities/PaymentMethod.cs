using System.Text.Json.Serialization;

namespace FPTU_ELibrary.Domain.Entities;

public class PaymentMethod
{
    public int PaymentMethodId { get; set; }
    public string MethodName { get; set; } = null!;

    [JsonIgnore]
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}