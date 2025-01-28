using FPTU_ELibrary.API.Payloads;
using FPTU_ELibrary.API.Payloads.Requests.Speech;
using FPTU_ELibrary.Application.Services.IServices;
using Microsoft.AspNetCore.Mvc;

namespace FPTU_ELibrary.API.Controllers;

public class SpeechController : ControllerBase
{
    private readonly IVoiceService _voiceService;
    
    public SpeechController(IVoiceService voiceService)
    {
        _voiceService = voiceService;
    }
    
    [HttpGet(APIRoute.AIServices.GetAvailableLanguages, Name = nameof(GetAvailableLanguages))]
    public async Task<IActionResult> GetAvailableLanguages()
    {
        return Ok(await _voiceService.GetLanguages());
    }
    
    [HttpPost(APIRoute.AIServices.VoiceSearching, Name = nameof(VoiceSearching))]
    public async Task<IActionResult> VoiceSearching([FromForm] VoiceSearchingRequest req)
    {
        return Ok(await _voiceService.VoiceToText(req.AudioFile, req.LanguageCode));
    }

}