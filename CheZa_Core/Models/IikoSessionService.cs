using Microsoft.AspNetCore.DataProtection;

namespace CheZa_Core.Models
{
    public interface IIikoSessionService
    {
        void SaveAccessToken(string token, DateTime expirationTime);
        (string Token, DateTime ExpirationTime) GetAccessToken();

        void SaveOrganizationId(string organizationId);
        string GetOrganizationId();

        void SaveTerminalGroupId(string terminalGroupId);
        string GetTerminalGroupId();

        void SaveTableId(string tableId);
        string GetTableId();
    }
    public class IikoSessionService : IIikoSessionService
    {
        private readonly IDataProtector _protector;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public IikoSessionService(IDataProtectionProvider provider, IHttpContextAccessor httpContextAccessor)
        {
            _protector = provider.CreateProtector("IikoSessionProtector");
            _httpContextAccessor = httpContextAccessor;
        }

        public void SaveAccessToken(string token, DateTime expirationTime)
        {
            var protectedToken = _protector.Protect(token);
            _httpContextAccessor.HttpContext.Session.SetString("AccessToken", protectedToken);
            _httpContextAccessor.HttpContext.Session.SetString("TokenExpirationTime", expirationTime.ToString("o")); // Using ISO 8601 format
        }

        public (string Token, DateTime ExpirationTime) GetAccessToken()
        {
            var protectedToken = _httpContextAccessor.HttpContext.Session.GetString("AccessToken");
            var tokenExpirationTimeStr = _httpContextAccessor.HttpContext.Session.GetString("TokenExpirationTime");
            DateTime expirationTime = DateTime.MinValue;

            if (DateTime.TryParse(tokenExpirationTimeStr, out var parsedTime))
            {
                expirationTime = parsedTime;
            }

            return (protectedToken == null ? null : _protector.Unprotect(protectedToken), expirationTime);
        }

        public void SaveOrganizationId(string organizationId)
        {
            _httpContextAccessor.HttpContext.Session.SetString("OrganizationId", organizationId);
        }

        public string GetOrganizationId()
        {
            return _httpContextAccessor.HttpContext.Session.GetString("OrganizationId");
        }

        public void SaveTerminalGroupId(string terminalGroupId)
        {
            _httpContextAccessor.HttpContext.Session.SetString("TerminalGroupId", terminalGroupId);
        }

        public string GetTerminalGroupId()
        {
            return _httpContextAccessor.HttpContext.Session.GetString("TerminalGroupId");
        }

        public void SaveTableId(string tableId)
        {
            _httpContextAccessor.HttpContext.Session.SetString("TableId", tableId);
        }

        public string GetTableId()
        {
            return _httpContextAccessor.HttpContext.Session.GetString("TableId");
        }
    }
}
