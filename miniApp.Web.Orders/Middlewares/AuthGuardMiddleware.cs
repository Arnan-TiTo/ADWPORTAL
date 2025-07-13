namespace miniApp.WebOrders.Middlewares
{

    public class AuthGuardMiddleware
    {
        private readonly RequestDelegate _next;

        public AuthGuardMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value?.ToLower();


            if (path != null && (
                path.StartsWith("/login") ||
                path.StartsWith("/register") ||
                path.StartsWith("/css") || path.StartsWith("/js") || path.StartsWith("/images") ||
                path.StartsWith("/favicon.ico") || path.StartsWith("/_framework") ||
                path.StartsWith("/api") || path.StartsWith("/session")
            ))
            {
                await _next(context);
                return;
            }


            if (!context.User.Identity?.IsAuthenticated ?? true)
            {
                context.Response.Redirect("/Login");
                return;
            }

            Console.WriteLine("Auth: " + context.User.Identity?.IsAuthenticated);
            Console.WriteLine("User: " + context.User.Identity?.Name);

            await _next(context);
        }
    }
}