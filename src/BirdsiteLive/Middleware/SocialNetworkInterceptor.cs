using System.Globalization;
using System.Threading.Tasks;
using BirdsiteLive.Twitter;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace BirdsiteLive.Middleware;

public class SocialNetworkInterceptor
{
    private readonly RequestDelegate _next;
    private readonly ITwitterUserService _twitterUserService;

    public SocialNetworkInterceptor(RequestDelegate next, ICachedTwitterUserService twitterUserService)
    {
        _next = next;
        _twitterUserService = twitterUserService;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        context.Items["UserService"] = _twitterUserService;

        // Call the next delegate/middleware in the pipeline.
        await _next(context);
    }
}

public static class RequestCultureMiddlewareExtensions
{
    public static IApplicationBuilder UseSocialNetworkInterceptor(
        this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<SocialNetworkInterceptor>();
    }
}