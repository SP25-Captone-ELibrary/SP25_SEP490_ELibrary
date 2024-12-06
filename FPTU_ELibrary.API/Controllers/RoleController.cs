using FPTU_ELibrary.API.Payloads;
using Microsoft.AspNetCore.Mvc;

namespace FPTU_ELibrary.API.Controllers;

[ApiController]
public class RoleController : ControllerBase
{
    [HttpGet(APIRoute.Role.GetAll, Name = nameof(GetAllRoleAsync))]
    public async Task<IActionResult> GetAllRoleAsync()
    {
        return Ok();
    }
}