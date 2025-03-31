using System.Security.Claims;
using FPTU_ELibrary.API.Payloads;
using FPTU_ELibrary.Application.Dtos.Recommendation;
using FPTU_ELibrary.Application.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FPTU_ELibrary.API.Controllers;

[ApiController]
public class RecommenderController : Controller
{
    private readonly IRecommenderService _recommenderSvc;

    public RecommenderController(IRecommenderService recommenderSvc)
    {
        _recommenderSvc = recommenderSvc;
    }
    
    [Authorize]
    [HttpGet(APIRoute.Recommender.GetUserRecommend, Name = nameof(GetUserRecommendAsync))]
    public async Task<IActionResult> GetUserRecommendAsync([FromQuery] RecommendFilterDto filter)
    {
        var email = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        return Ok(await _recommenderSvc.GetRecommendedItemAsync(
            email: email ?? string.Empty,
            filter: filter));
    }
}