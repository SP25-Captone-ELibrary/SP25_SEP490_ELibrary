using FPTU_ELibrary.API.Payloads;
using Microsoft.AspNetCore.Mvc;

namespace FPTU_ELibrary.API.Controllers;

[ApiController]
public class ReturnController : ControllerBase
{
    [HttpPut(APIRoute.Return.InLibraryReturn, Name = nameof(InLibraryReturnAsync))]
    public async Task<IActionResult> InLibraryReturnAsync()
    {
        return Ok();
    }
}