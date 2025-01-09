using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Dtos.Locations;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using FPTU_ELibrary.Domain.Specifications;
using MapsterMapper;
using Serilog;

namespace FPTU_ELibrary.Application.Services;

public class LibrarySectionService : GenericService<LibrarySection, LibrarySectionDto, int>, 
    ILibrarySectionService<LibrarySectionDto>
{
    public LibrarySectionService(
        ISystemMessageService msgService, 
        IUnitOfWork unitOfWork, 
        IMapper mapper, 
        ILogger logger) : base(msgService, unitOfWork, mapper, logger)
    {
    }
    
    public async Task<IServiceResult> GetAllByZoneIdAsync(int zoneId)
    {
        try
        {
            // Build spec
            var baseSpec = new BaseSpecification<LibrarySection>(lz => lz.ZoneId == zoneId);
            var entities = await _unitOfWork.Repository<LibrarySection, int>().GetAllWithSpecAsync(baseSpec);

            if (!entities.Any())
            {
                return new ServiceResult(ResultCodeConst.SYS_Warning0004, 
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004), 
                    _mapper.Map<IEnumerable<LibrarySectionDto>>(entities));
            }

            return new ServiceResult(ResultCodeConst.SYS_Success0002, 
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), 
                _mapper.Map<IEnumerable<LibrarySectionDto>>(entities));
        }
        catch(Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when progress get all data");
        }
    }
}