using System.ComponentModel;

namespace FPTU_ELibrary.Domain.Common.Enums;

public enum AITrainingStatus
{
    /// <summary>
    /// AI Training is processing
    /// </summary>
    [Description("Đang xử lý")]
    InProgress,
    
    /// <summary>
    /// AI Training is completed
    /// </summary>
    [Description("Hoàn thành")]
    Completed,
    
    /// <summary>
    /// AI Training is failed
    /// </summary>
    [Description("Thất bại")]
    Failed
}