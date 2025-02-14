using FPTU_ELibrary.Application.Dtos.Payments;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using MapsterMapper;
using Serilog;
using Transaction = FPTU_ELibrary.Domain.Entities.Transaction;

namespace FPTU_ELibrary.Application.Services;

public class TransactionService : GenericService<Transaction, TransactionDto, int>
    , ITransactionService<TransactionDto>
{
    public TransactionService(ILogger logger,
        ISystemMessageService msgService,
        IUnitOfWork unitOfWork,
        IMapper mapper) : base(msgService, unitOfWork,
        mapper, logger)
    {
    }
    
    public async Task<IServiceResult> CreateAsync(TransactionDto dto)
    {
        
    }
}