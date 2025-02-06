using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;

namespace FPTU_ELibrary.Domain.Interfaces.Services;

public interface IBorrowRecordService<TDto> : IGenericService<BorrowRecord, TDto, int>
    where TDto : class
{
}