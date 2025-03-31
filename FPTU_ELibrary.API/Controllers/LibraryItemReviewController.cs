using System.Security.Claims;
using FPTU_ELibrary.API.Extensions;
using FPTU_ELibrary.API.Payloads;
using FPTU_ELibrary.API.Payloads.Requests.LibraryItem;
using FPTU_ELibrary.Application.Dtos.LibraryItems;
using FPTU_ELibrary.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FPTU_ELibrary.API.Controllers;

[ApiController]
public class LibraryItemReviewController : ControllerBase
{
    private readonly ILibraryItemReviewService<LibraryItemReviewDto> _itemReviewSvc;

    public LibraryItemReviewController(
        ILibraryItemReviewService<LibraryItemReviewDto> itemReviewSvc
    )
    {
        _itemReviewSvc = itemReviewSvc;
    }
    
    [Authorize]
    [HttpGet(APIRoute.LibraryItemReview.GetByItemId, Name = nameof(GetLibraryItemReviewByItemIdAsync))]
    public async Task<IActionResult> GetLibraryItemReviewByItemIdAsync([FromRoute] int libraryItemId)
    {
        var email = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        return Ok(await _itemReviewSvc.GetItemReviewByEmailAsync(email: email ?? string.Empty, libraryItemId: libraryItemId));
    }

    [Authorize]
    [HttpPost(APIRoute.LibraryItemReview.Review, Name = nameof(ReviewItemAsync))]
    public async Task<IActionResult> ReviewItemAsync([FromBody] ReviewItemRequest req)
    {
        var email = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        return Ok(await _itemReviewSvc.ReviewItemAsync(email: email ?? string.Empty, dto: req.ToLibraryItemReviewDto()));
    }

    [Authorize]
    [HttpDelete(APIRoute.LibraryItemReview.Delete, Name = nameof(DeleteLibraryItemReviewAsync))]
    public async Task<IActionResult> DeleteLibraryItemReviewAsync([FromRoute] int libraryItemId)
    {
        var email = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        return Ok(await _itemReviewSvc.DeleteAsync(email: email ?? string.Empty, libraryItemId: libraryItemId));
    }
}