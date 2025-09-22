using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;

namespace adwportal.Security
{
    public class AuthMessageHandler : DelegatingHandler
    {
        private readonly IHttpContextAccessor _http;
        private readonly AuthTokenProvider _fixed;
        public AuthMessageHandler(IHttpContextAccessor http, AuthTokenProvider fixedToken)
        { _http = http; _fixed = fixedToken; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage req, CancellationToken ct)
        {
            var jwt = _http.HttpContext?.Session.GetString("JWT");
            if (!string.IsNullOrWhiteSpace(jwt))
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

            if (!string.IsNullOrWhiteSpace(_fixed.Token))
                req.Headers.TryAddWithoutValidation("X-AuthToken", _fixed.Token);

            return await base.SendAsync(req, ct);
        }
    }
}
