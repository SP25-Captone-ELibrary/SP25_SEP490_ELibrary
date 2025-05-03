using System.Globalization;
using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Configurations;
using FPTU_ELibrary.Application.Dtos.AIServices.Speech;
using FPTU_ELibrary.Application.Dtos.LibraryItems;
using FPTU_ELibrary.Application.Extensions;
using FPTU_ELibrary.Application.Services.IServices;
using FPTU_ELibrary.Application.Utils;
using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using Microsoft.AspNetCore.Http;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.Extensions.Options;
using NAudio.Lame;
using NAudio.Wave;
using Serilog;
using Xabe.FFmpeg;

namespace FPTU_ELibrary.Application.Services;

public class VoiceService : IVoiceService
{
    private readonly SpeechConfig _speechConfig;

    // private readonly IBookService<BookDto> _bookService;
    private readonly ILibraryItemService<LibraryItemDto> _editionService;
    private readonly ISearchService _searchService;
    private readonly FFMPEGSettings _audioMonitor;
    private readonly AdsScriptSettings _adsMonitor;
    private readonly ILogger _logger;
    private readonly ISystemMessageService _msgService;
    private readonly AzureSpeechSettings _monitor;

    public VoiceService(SpeechConfig speechConfig,
        // IBookService<BookDto> bookService
        ILibraryItemService<LibraryItemDto> editionService,
        ISearchService searchService, IOptionsMonitor<AzureSpeechSettings> monitor,
        IOptionsMonitor<AdsScriptSettings> adsMonitor,
        IOptionsMonitor<FFMPEGSettings> audioMonitor,
        ILogger logger, ISystemMessageService msgService)
    {
        _speechConfig = speechConfig;
        _editionService = editionService;
        _searchService = searchService;
        _audioMonitor = audioMonitor.CurrentValue;
        _adsMonitor = adsMonitor.CurrentValue;
        _logger = logger;
        _msgService = msgService;
        _monitor = monitor.CurrentValue;
    }

    public async Task<IServiceResult> VoiceToText(IFormFile audioFile, string languageCode)
    {
        var tempFilePath = Path.GetTempFileName();
        try
        {
            using (var fileStream = new FileStream(tempFilePath, FileMode.Create))
            {
                await audioFile.CopyToAsync(fileStream);
            }

            // Create AudioConfig from the temporary WAV file
            using var audioInput = AudioConfig.FromWavFileInput(tempFilePath);
            using var recognizer = new SpeechRecognizer(_speechConfig, languageCode, audioInput);

            // Perform speech recognition
            var result = await recognizer.RecognizeOnceAsync();
            var recogniseTitle = StringUtils.SplitSpecialCharAtTheEnd(result.Text);

            return new ServiceResult(ResultCodeConst.SYS_Success0002,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), recogniseTitle);
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when Train Book Model");
        }
    }

    public async Task<IServiceResult> GetLanguages()
    {
        try
        {
            List<SpeechLanguagesDto> availableLanguages = new List<SpeechLanguagesDto>();
            availableLanguages = _monitor.Languages
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(langCode =>
                {
                    var trimmedCode = langCode.Trim();
                    try
                    {
                        var culture = new CultureInfo(trimmedCode);
                        return new SpeechLanguagesDto()
                        {
                            LanguageCode = trimmedCode,
                            LanguageName = culture.DisplayName
                        };
                    }
                    catch (CultureNotFoundException)
                    {
                        return new SpeechLanguagesDto()
                        {
                            LanguageCode = trimmedCode,
                            LanguageName = "Unknown Language"
                        };
                    }
                })
                .ToList();
            return new ServiceResult(ResultCodeConst.SYS_Success0002,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), availableLanguages);
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when Train Book Model");
        }
    }

    public async Task<IServiceResult> TextToVoice(string lang, string email)
    {
        _speechConfig.SpeechSynthesisVoiceName = lang.ToLower() switch
        {
            "vi" => "vi-VN-HoaiMyNeural",
            "en" => "en-US-AriaNeural",
            _ => "vi-VN-HoaiMyNeural"
		};

        using var audioStream = AudioOutputStream.CreatePullStream();
        using var audioConfig = AudioConfig.FromStreamOutput(audioStream);
        using var synthesizer = new SpeechSynthesizer(_speechConfig, audioConfig);

        var script = lang.ToLower().Equals("en") ? _adsMonitor.En : _adsMonitor.Vi;
        var editedScript = StringUtils.Format(script, email);
        var result = await synthesizer.SpeakTextAsync(editedScript);

        if (result.Reason != ResultReason.SynthesizingAudioCompleted)
        {
            throw new Exception($"Text-to-Speech failed: {result.Reason}");
        }

        var memoryStream = new MemoryStream(result.AudioData);
        memoryStream.Position = 0;

        return new ServiceResult(ResultCodeConst.SYS_Success0002,
            await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002),
            memoryStream);
    }

    async Task<MemoryStream> ConvertWavToMp3Async(Stream wavStream)
    {
        var mp3Stream = new MemoryStream();

        using (var reader = new WaveFileReader(wavStream))
        using (var writer = new LameMP3FileWriter(mp3Stream, reader.WaveFormat, LAMEPreset.VBR_90))
        {
            await reader.CopyToAsync(writer);
        }

        mp3Stream.Position = 0;
        return mp3Stream;
    }

    public async Task<IServiceResult> TextToVoiceFile(string email)
    {
        var sysLang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
            LanguageContext.CurrentLanguage);
        
        _speechConfig.SpeechSynthesisVoiceName = sysLang switch
        {
            SystemLanguage.Vietnamese => "vi-VN-HoaiMyNeural",
            SystemLanguage.English => "en-US-AriaNeural",
            _ => "vi-VN-HoaiMyNeural"
		};

        using var audioStream = AudioOutputStream.CreatePullStream();
        using var audioConfig = AudioConfig.FromStreamOutput(audioStream);
        using var synthesizer = new SpeechSynthesizer(_speechConfig, audioConfig);

        var script = sysLang == SystemLanguage.English
            ? _adsMonitor.En
            : _adsMonitor.Vi;
        var editedScript = StringUtils.Format(script, email);
        var result = await synthesizer.SpeakTextAsync(editedScript);

        if (result.Reason != ResultReason.SynthesizingAudioCompleted)
        {
            throw new Exception($"Text-to-Speech failed: {result.Reason}");
        }

        // Convert WAV to MP3 in memory
        var wavMemoryStream = new MemoryStream(result.AudioData);
        // byte[] mp3Data = ConvertWavToMp3(wavMemoryStream.ToArray());

        // var mp3Stream = new MemoryStream(mp3Data);
        //     return new ServiceResult(ResultCodeConst.SYS_Success0002,
        //         await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002),
        //         mp3Stream);
        return new ServiceResult(ResultCodeConst.SYS_Success0002,
            await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002),
            wavMemoryStream);
    }

// Convert WAV to MP3
    // private static byte[] ConvertWavToMp3(byte[] wavFile)
    // {
    //     using var retMs = new MemoryStream();
    //     using var ms = new MemoryStream(wavFile);
    //     using var rdr = new WaveFileReader(ms);
    //     using var wtr = new LameMP3FileWriter(retMs, rdr.WaveFormat, LAMEPreset.STANDARD);
    //
    //     rdr.CopyTo(wtr);
    //     wtr.Flush();
    //
    //     return retMs.ToArray();
    // }
    private static byte[] ConvertWavToMp3(byte[] wavFile)
    {
        using var retMs = new MemoryStream();
        using var ms = new MemoryStream(wavFile);
        using var rdr = new WaveFileReader(ms);

        // 🔹 Resample to 44100Hz, Stereo
        using var resampler = new MediaFoundationResampler(rdr, new WaveFormat(44100, 2))
        {
            ResamplerQuality = 60
        };

        using var wtr = new LameMP3FileWriter(retMs, resampler.WaveFormat, LAMEPreset.STANDARD);

        // 🔹 Manually copy data
        byte[] buffer = new byte[4096];
        int bytesRead;
        while ((bytesRead = resampler.Read(buffer, 0, buffer.Length)) > 0)
        {
            wtr.Write(buffer, 0, bytesRead);
        }

        wtr.Flush();
        return retMs.ToArray();
    }
}