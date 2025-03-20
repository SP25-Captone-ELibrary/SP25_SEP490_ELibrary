using System.ComponentModel;

namespace FPTU_ELibrary.Domain.Common.Enums;

public enum FinePolicyConditionType
{
    [Description("Trễ hạn")]
    OverDue,
    
    [Description("Mất")]
    Lost,
    
    [Description("Hư hỏng")]
    Damage,
    
    // Add other condition types here...
}
