using FPTU_ELibrary.Application.Dtos.Payments;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces;
using FPTU_ELibrary.Domain.Interfaces.Services;
using MapsterMapper;
using Serilog;

namespace FPTU_ELibrary.Application.Services;

public class PaymentMethodService : GenericService<PaymentMethod, PaymentMethodDto, int>,
    IPaymentMethodService<PaymentMethodDto>
{
    public PaymentMethodService(
        ISystemMessageService msgService, 
        IUnitOfWork unitOfWork, 
        IMapper mapper, 
        ILogger logger) : base(msgService, unitOfWork, mapper, logger)
    {
    }
}