using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Dtos.Locations;
using FPTU_ELibrary.Application.Extensions;
using FPTU_ELibrary.Application.Utils;
using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using FPTU_ELibrary.Domain.Specifications;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Utilities;
using Serilog;

namespace FPTU_ELibrary.Application.Services;

public class LibraryFloorService : GenericService<LibraryFloor, LibraryFloorDto, int>, 
    ILibraryFloorService<LibraryFloorDto>
{
    public LibraryFloorService(
        ISystemMessageService msgService, 
        IUnitOfWork unitOfWork, 
        IMapper mapper, 
        ILogger logger) : base(msgService, unitOfWork, mapper, logger)
    {
    }

    public async Task<IServiceResult> GetMapByFloorIdAsync(int floorId)
    {
        try
        {
            // Determine current system lang 
            var lang = (SystemLanguage?) EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;
            
            // Build spec
            var baseSpec = new BaseSpecification<LibraryFloor>(f => f.FloorId == floorId);
            // Apply include
            baseSpec.ApplyInclude(q => q
                .Include(f => f.LibraryZones) // Library zones
                    .ThenInclude(z => z.LibrarySections) // Library sections 
                        .ThenInclude(s => s.LibraryShelves) // Library shelves
            );
            // Retrieve data with spec
            var existingEntity = await _unitOfWork.Repository<LibraryFloor, int>().GetWithSpecAsync(baseSpec);
            if (existingEntity == null)
            {
                // Not found {0}
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002, 
                    StringUtils.Format(errMsg, isEng ? "floor information" : "thông tin tầng lầu"));
            }
            
            return new ServiceResult(ResultCodeConst.SYS_Success0002,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002),
                _mapper.Map<LibraryFloor>(existingEntity));
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process get map by floor id");
        }
    }
}