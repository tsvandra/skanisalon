using Microsoft.EntityFrameworkCore;
using Soluvion.API.Data;
using Soluvion.API.Interfaces;

namespace Soluvion.API.Middleware
{
    public class TenantResolutionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<TenantResolutionMiddleware> _logger;

        public TenantResolutionMiddleware(RequestDelegate next, ILogger<TenantResolutionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext, AppDbContext db, IConfiguration configuration)
        {
            int? tenantId = null;
            string? domain = null;

            // 1. Domain kinyerése a kérésből (fejléc, Origin, Referer vagy Host alapján)
            if (context.Request.Headers.TryGetValue("X-Tenant-Domain", out var tenantDomainHeader) && !string.IsNullOrWhiteSpace(tenantDomainHeader))
            {
                domain = tenantDomainHeader.ToString().Trim();
            }
            else if (context.Request.Headers.TryGetValue("Origin", out var originHeader) && Uri.TryCreate(originHeader, UriKind.Absolute, out var originUri))
            {
                domain = originUri.Host;
            }
            else if (context.Request.Headers.TryGetValue("Referer", out var refererHeader) && Uri.TryCreate(refererHeader, UriKind.Absolute, out var refererUri))
            {
                domain = refererUri.Host;
            }
            else
            {
                domain = context.Request.Host.Host;
            }

            bool isDevOrTestHost = string.IsNullOrEmpty(domain)
                || domain == "localhost"
                || domain == "127.0.0.1"
                || domain.EndsWith(".netlify.app", StringComparison.OrdinalIgnoreCase)
                || domain.EndsWith(".up.railway.app", StringComparison.OrdinalIgnoreCase);

            // 2. Hitelesített felhasználó tokenjéből (ClaimTypes / "CompanyId")
            // Ha a felhasználó be van jelentkezve, a JWT CompanyId a legbiztonságosabb forrás
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var companyClaim = context.User.FindFirst("CompanyId") ?? context.User.FindFirst("companyId");
                if (companyClaim != null && int.TryParse(companyClaim.Value, out int claimCompanyId) && claimCompanyId > 0)
                {
                    tenantId = claimCompanyId;
                }
            }

            // 3. Fejlesztői felülbírálás KIZÁRÓLAG teszt/fejlesztői környezetben engedélyezett!
            // Éles domaineken (pl. skanisalon.sk) a forceTenant és a manuális header SZIGORÚAN TILTVA van.
            if (tenantId == null && isDevOrTestHost)
            {
                if (context.Request.Query.TryGetValue("forceTenant", out var forceTenantValue)
                    && int.TryParse(forceTenantValue, out int parsedId) && parsedId > 0)
                {
                    tenantId = parsedId;
                }
                else if (context.Request.Headers.TryGetValue("X-Tenant-ID", out var headerTenant)
                    && int.TryParse(headerTenant, out int id1) && id1 > 0)
                {
                    tenantId = id1;
                }
                else if (context.Request.Headers.TryGetValue("X-Company-Id", out var headerCompany)
                    && int.TryParse(headerCompany, out int id2) && id2 > 0)
                {
                    tenantId = id2;
                }
            }

            // --- ADATBÁZIS KERESÉS (Mindig IgnoreQueryFilters-szel, hogy a cég elérhető legyen) ---
            if (tenantId.HasValue)
            {
                var company = await db.Companies
                    .IgnoreQueryFilters()
                    .Include(c => c.Languages)
                    .FirstOrDefaultAsync(c => c.Id == tenantId.Value && !c.IsDeleted);

                if (company != null)
                {
                    tenantContext.CurrentCompany = company;
                }
                else
                {
                    _logger.LogWarning("Nem található aktív cég a megadott TenantId alapján: {TenantId}", tenantId.Value);
                }
            }
            else if (!string.IsNullOrEmpty(domain) && domain != "localhost" && domain != "127.0.0.1")
            {
                domain = domain.ToLowerInvariant();
                if (domain.StartsWith("www."))
                {
                    domain = domain.Substring(4);
                }

                // Keresés pontos domain alapján
                var company = await db.Companies
                    .IgnoreQueryFilters()
                    .Include(c => c.Languages)
                    .FirstOrDefaultAsync(c => c.Domain != null && (c.Domain == domain || c.Domain == "www." + domain) && !c.IsDeleted);

                // Aldomain feloldás (pl. skani.soluvion.app -> "skani")
                if (company == null && domain.Contains('.'))
                {
                    var parts = domain.Split('.');
                    if (parts.Length >= 2)
                    {
                        var subdomain = parts[0];
                        company = await db.Companies
                            .IgnoreQueryFilters()
                            .Include(c => c.Languages)
                            .FirstOrDefaultAsync(c => c.Domain == subdomain && !c.IsDeleted);
                    }
                }

                if (company != null)
                {
                    tenantContext.CurrentCompany = company;
                }
                else
                {
                    _logger.LogWarning("Nem található aktív cég a megadott domain alapján: {Domain}", domain);
                }
            }

            // 5. Fallback DefaultTenantId konfigurációból (kivéve Auth végpontok, ahol új céget regisztrálnak)
            var requestPath = context.Request.Path.Value?.ToLowerInvariant() ?? "";
            bool isAuthEndpoint = requestPath.StartsWith("/api/auth");

            if (tenantContext.CurrentCompany == null && !isAuthEndpoint)
            {
                var defaultTenantId = configuration.GetValue<int?>("AppSettings:DefaultTenantId");
                if (defaultTenantId.HasValue && defaultTenantId.Value > 0)
                {
                    var fallbackCompany = await db.Companies
                        .IgnoreQueryFilters()
                        .Include(c => c.Languages)
                        .FirstOrDefaultAsync(c => c.Id == defaultTenantId.Value && !c.IsDeleted);

                    if (fallbackCompany != null)
                    {
                        tenantContext.CurrentCompany = fallbackCompany;
                    }
                }
            }

            await _next(context);
        }
    }
}