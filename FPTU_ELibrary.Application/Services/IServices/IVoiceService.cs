using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using Microsoft.AspNetCore.Http;

namespace FPTU_ELibrary.Application.Services.IServices;

public interface IVoiceService
{
    Task<IServiceResult> VoiceToText(IFormFile audioFile,string languageCode);
    Task<IServiceResult> GetLanguages();
    Task<IServiceResult> TextToVoice(string lang, string email);
    Task<IServiceResult> TextToVoiceFile(string lang, string email);
}