using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

public class SessionTimeoutMiddleware
{
    private readonly RequestDelegate _next;
    private readonly TimeSpan _timeout = TimeSpan.FromMinutes(1); // configurable

    public SessionTimeoutMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // 🚫 Skip if session feature is missing
        var sessionFeature = context.Features.Get<ISessionFeature>();
        if (sessionFeature == null)
        {
            await _next(context);
            return;
        }

        // 🚫 Skip static files & public pages
        var path = context.Request.Path.Value?.ToLower() ?? string.Empty;
        if (IsIgnoredPath(path))
        {
            await _next(context);
            return;
        }

        // 🚫 Session not available yet
        if (!context.Session.IsAvailable)
        {
            await _next(context);
            return;
        }

        var lastActivityStr = context.Session.GetString("LastActivity");

        if (!string.IsNullOrEmpty(lastActivityStr) &&
            DateTime.TryParse(lastActivityStr, out var lastActivity))
        {
            if (DateTime.UtcNow - lastActivity > _timeout)
            {
                context.Session.Clear();

                // 🔹 AJAX request
                if (IsAjaxRequest(context))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsync("SessionExpired");
                }
                else
                {
                    context.Response.Redirect(
                        "/AccessedError/Index?code=440&message=Session%20expired.%20Please%20login%20again");
                }

                return;
            }
        }

        // ✅ Update activity timestamp
        context.Session.SetString("LastActivity", DateTime.UtcNow.ToString("O"));

        await _next(context);
    }

    private static bool IsAjaxRequest(HttpContext context)
    {
        return context.Request.Headers["X-Requested-With"] == "XMLHttpRequest";
    }

    private static bool IsIgnoredPath(string path)
    {
        return path.StartsWith("/homelogin")
            || path.StartsWith("/login")
            || path.StartsWith("/accessederror")
            || path.StartsWith("/css")
            || path.StartsWith("/js")
            || path.StartsWith("/images")
            || path.StartsWith("/lib");
    }
}


//using Microsoft.AspNetCore.Http.Features;

//public class SessionTimeoutMiddleware
//{
//    private readonly RequestDelegate _next;

//    public SessionTimeoutMiddleware(RequestDelegate next)
//    {
//        _next = next;
//    }
//    public async Task Invoke(HttpContext context)
//    {
//        if (context.Features.Get<ISessionFeature>() == null)
//        {
//            await _next(context);
//            return;
//        }
//        if (!context.Session.IsAvailable)
//        {
//            await _next(context);
//            return;
//        }

//        var path = context.Request.Path.Value?.ToLower() ?? "";
//        if (path.StartsWith("/homelogin") ||
//            path.StartsWith("/login") ||
//            path.StartsWith("/accessederror") ||
//            path.StartsWith("/css") ||
//            path.StartsWith("/js") ||
//            path.StartsWith("/images"))
//        {
//            await _next(context);
//            return;
//        }

//        var lastActivity = context.Session.GetString("LastActivity");

//        if (!string.IsNullOrEmpty(lastActivity))
//        {
//            var lastTime = DateTime.Parse(lastActivity);

//            if ((DateTime.Now - lastTime).TotalMinutes > 1)
//            {
//                context.Session.Clear();

//                // AJAX request
//                if (context.Request.Headers["X-Requested-With"] == "XMLHttpRequest")
//                {
//                    context.Response.StatusCode = 401;
//                    await context.Response.WriteAsync("SessionExpired");
//                }
//                else
//                {
//                    context.Response.Redirect("/AccessedError/Index");
//                }
//                return;
//            }
//        }

//        context.Session.SetString("LastActivity", DateTime.Now.ToString());
//        await _next(context);
//    }
//}
