namespace adwportal.Services
{
    public class AuthTokenProvider
    {
        private readonly IHttpContextAccessor _http;
        private readonly string? _fixedToken;
        private readonly TokenCacheService? _tokenCache;

        public AuthTokenProvider(IHttpContextAccessor http, TokenCacheService? tokenCache = null)
        {
            _http = http;
            _tokenCache = tokenCache;
            // fixed token สำหรับ Portal API (ถ้าใช้)
            _fixedToken = Environment.GetEnvironmentVariable("AuthToken", EnvironmentVariableTarget.Machine);
        }

        private ISession? Session => _http.HttpContext?.Session;

        // ========== Portal JWT ==========
        private string? _token;
        public string Token
        {
            get
            {
                if (!string.IsNullOrEmpty(_token))
                    return _token;

                var s = Session?.GetString("JWT") ?? _fixedToken ?? string.Empty;
                _token = s;
                return s;
            }
            set
            {
                _token = value ?? string.Empty;
                if (Session != null)
                    Session.SetString("JWT", _token);
            }
        }

        // ========== IDW TOKEN (Delegated to TokenCacheService) ==========
        public string? IdwToken
        {
            get
            {
                // Use TokenCacheService if available
                if (_tokenCache != null)
                {
                    return _tokenCache.GetIdwTokenAsync().GetAwaiter().GetResult();
                }

                // Fallback to session (for backward compatibility)
                var s = Session?.GetString("IDW_TOKEN");
                return s;
            }
            set
            {
                // Still allow setting for backward compatibility
                if (Session == null) return;

                if (string.IsNullOrEmpty(value))
                    Session.Remove("IDW_TOKEN");
                else
                    Session.SetString("IDW_TOKEN", value);
            }
        }

        // ========== MDW TOKEN (Delegated to TokenCacheService) ==========
        public string? MdwToken
        {
            get
            {
                // Use TokenCacheService if available
                if (_tokenCache != null)
                {
                    return _tokenCache.GetMdwTokenAsync().GetAwaiter().GetResult();
                }

                // Fallback to session (for backward compatibility)
                var s = Session?.GetString("MDW_TOKEN");
                return s;
            }
            set
            {
                // Still allow setting for backward compatibility
                if (Session == null) return;

                if (string.IsNullOrEmpty(value))
                    Session.Remove("MDW_TOKEN");
                else
                    Session.SetString("MDW_TOKEN", value);
            }
        }

        // ========== PASSWORD (Session Only) ==========
        private string? _password;
        public string? Password
        {
            get
            {
                if (!string.IsNullOrEmpty(_password)) return _password;
                var s = Session?.GetString("PWD");
                if (!string.IsNullOrEmpty(s)) _password = s;
                return _password;
            }
            set
            {
                _password = value;
                if (Session != null)
                {
                    if (string.IsNullOrEmpty(value)) Session.Remove("PWD");
                    else Session.SetString("PWD", value);
                }
            }
        }
    }

}
