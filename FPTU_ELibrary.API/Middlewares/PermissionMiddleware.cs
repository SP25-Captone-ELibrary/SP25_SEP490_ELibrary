using System.Security.Claims;
using FPTU_ELibrary.Application.Exceptions;
using FPTU_ELibrary.Domain.Interfaces.Services;

namespace FPTU_ELibrary.API.Middlewares;

public class PermissionMiddleware
{
    private readonly RequestDelegate _next;

    public PermissionMiddleware(RequestDelegate next)
    {
        _next = next;
    }
    
    public async Task InvokeAsync(HttpContext context, IAuthorizationService authService)
    {
        // Extract route and HTTP verb
        var path = context.Request.Path.Value?.ToLower();
        var method = context.Request.Method;
        var featureDesc = GetFeatureFromPath(path);

        if (!string.IsNullOrEmpty(featureDesc))
        {
            var user = context.User;
            var role = user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;

            if (role != null)
            {
                // Check permission
                var isAuthorized = await authService.IsAuthorizedAsync(role, featureDesc, method);
                if (!isAuthorized)
                {
                    throw new ForbiddenException("You do not have permission to access this resource.");
                }
            }
        }

        await _next(context);
    }
    
    private string? GetFeatureFromPath(string? path)
    {
        if (path == null) return null;
        var segments = path.Split('/');
        return segments.Length > 2 ? segments[2] : null; // "roles", "books", etc.
    }
}