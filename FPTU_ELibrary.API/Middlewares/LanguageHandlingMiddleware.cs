using FPTU_ELibrary.Application.Common;
using ILogger = Serilog.ILogger;

namespace FPTU_ELibrary.API.Middlewares;

//  Summary:
//      This class is to handle retrieving Accept-Language in
//      request header for the application
public class LanguageHandlingMiddleware
{
    // Func that can process HTTP request
    private readonly RequestDelegate _next;
    private readonly ILogger _logger;

    public LanguageHandlingMiddleware(
        ILogger logger,
        RequestDelegate next)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Try to retrieve global language
        SetLanguageContext(context);
            
        // Proceed to the next middleware
        await _next(context);
    }
    
    //	Summary:
    //		Set language context
    private void SetLanguageContext(HttpContext context)
    {
        // Retrieve and set language context from request headers
        var language = context.Request.Headers["Accept-Language"].FirstOrDefault();
        LanguageContext.CurrentLanguage = language != null && !language.Split(";").Any() ? language : "en";
        _logger.Information("Language set to: {Language}", language);
    }
}