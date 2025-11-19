namespace adwportal.Services
{
    public class AuthTokenProvider
    {
        private readonly IHttpContextAccessor _http;
        private readonly string? _fixedToken;

        public AuthTokenProvider(IHttpContextAccessor http)
        {
            _http = http;
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

        // ========== IDW TOKEN ==========
        private string? _idwToken;
        public string? IdwToken
        {
            get
            {
                if (!string.IsNullOrEmpty(_idwToken))
                    return _idwToken;

                var s = Session?.GetString("IDW_TOKEN");
                if (!string.IsNullOrEmpty(s))
                    _idwToken = s;

                return _idwToken;
            }
            set
            {
                _idwToken = value;

                if (Session == null) return;

                if (string.IsNullOrEmpty(value))
                    Session.Remove("IDW_TOKEN");
                else
                    Session.SetString("IDW_TOKEN", value);
            }
        }

        // ========== MDW TOKEN ==========
        private string? _mdwToken;
        public string? MdwToken
        {
            get
            {
                if (!string.IsNullOrEmpty(_mdwToken))
                    return _mdwToken;

                var s = Session?.GetString("MDW_TOKEN");
                if (!string.IsNullOrEmpty(s))
                    _mdwToken = s;

                return _mdwToken;
            }
            set
            {
                _mdwToken = value;

                if (Session == null) return;

                if (string.IsNullOrEmpty(value))
                    Session.Remove("MDW_TOKEN");
                else
                    Session.SetString("MDW_TOKEN", value);
            }
        }
    }

}
