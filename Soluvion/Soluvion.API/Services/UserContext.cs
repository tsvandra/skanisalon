using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Soluvion.API.Data;
using Soluvion.API.Interfaces;
using Soluvion.Domain.Models;

namespace Soluvion.API.Services
{
    public class UserContext : IUserContext
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly AppDbContext _context;
        private readonly ITenantContext _tenantContext;
        private CompanyEmployee? _cachedEmployee;
        private bool _employeeLoaded;

        public UserContext(IHttpContextAccessor httpContextAccessor, AppDbContext context, ITenantContext tenantContext)
        {
            _httpContextAccessor = httpContextAccessor;
            _context = context;
            _tenantContext = tenantContext;
        }

        private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

        public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;

        public int? UserId
        {
            get
            {
                var claim = User?.FindFirst(ClaimTypes.NameIdentifier) ?? User?.FindFirst("sub") ?? User?.FindFirst("userId");
                if (claim != null && int.TryParse(claim.Value, out int id) && id > 0)
                {
                    return id;
                }
                return null;
            }
        }

        public string? Username => User?.FindFirst(ClaimTypes.Name)?.Value;

        public string? Role => User?.FindFirst(ClaimTypes.Role)?.Value;

        public int? CompanyId
        {
            get
            {
                var claim = User?.FindFirst("CompanyId") ?? User?.FindFirst("companyId");
                if (claim != null && int.TryParse(claim.Value, out int id) && id > 0)
                {
                    return id;
                }
                return _tenantContext.CurrentCompany?.Id;
            }
        }

        public async Task<CompanyEmployee?> GetCurrentEmployeeAsync()
        {
            if (_employeeLoaded) return _cachedEmployee;

            _employeeLoaded = true;
            int companyId = _tenantContext.CurrentCompany?.Id ?? CompanyId ?? 0;
            if (companyId <= 0) return null;

            int? userId = UserId;
            if (userId.HasValue && userId.Value > 0)
            {
                _cachedEmployee = await _context.CompanyEmployees
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(e => e.UserId == userId.Value && e.CompanyId == companyId);
            }
            else if (!string.IsNullOrEmpty(Username))
            {
                var user = await _context.Users
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(u => u.Username == Username);

                if (user != null)
                {
                    _cachedEmployee = await _context.CompanyEmployees
                        .IgnoreQueryFilters()
                        .FirstOrDefaultAsync(e => e.UserId == user.Id && e.CompanyId == companyId);
                }
            }

            return _cachedEmployee;
        }
    }
}

