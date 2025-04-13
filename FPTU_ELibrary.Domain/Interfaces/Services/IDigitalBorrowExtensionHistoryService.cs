using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;

namespace FPTU_ELibrary.Domain.Interfaces.Services;

public interface IDigitalBorrowExtensionHistoryService<TDto> : IReadOnlyService<DigitalBorrowExtensionHistory, TDto, int>
    where TDto : class
{
}