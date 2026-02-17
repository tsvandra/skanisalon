using Microsoft.EntityFrameworkCore;
using Soluvion.API.Data;
using Soluvion.API.Services;

namespace Soluvion.API.Middleware
{
    public class TenantResolutionMiddleware
    {
        private readonly RequestDelegate _next;

        public TenantResolutionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        // Fontos: A Scoped service-eket (AppDbContext, ITenantContext) itt, az InvokeAsync-ban kérjük el,
        // NEM a konstruktorban, mert a Middleware maga Singleton (egyszer jön létre), de a DbContext kérésenként új.
        public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext, AppDbContext db)
        {
            int? tenantId = null;
            string? domain = null;

            // 1. Fejlesztői felülbírálás Query Paraméterrel (pl. ?forceTenant=2)
            if (context.Request.Query.TryGetValue("forceTenant", out var forceTenantValue))
            {
                if (int.TryParse(forceTenantValue, out int parsedId))
                {
                    tenantId = parsedId;
                }
            }

            // 2. API Tesztelés Headerrel (pl. X-Tenant-ID: 2)
            if (tenantId == null && context.Request.Headers.TryGetValue("X-Tenant-ID", out var headerTenantValue))
            {
                if (int.TryParse(headerTenantValue, out int parsedId))
                {
                    tenantId = parsedId;
                }
            }

            // 3. Domain alapú feloldás (Production)
            // Ha nincs ID megadva, megnézzük a hívó Host-ot (pl. skanisalon.sk)
            if (tenantId == null)
            {
                // A Host tartalmazhat portot is (localhost:5000), azt levágjuk ha kell, de most egyszerűsítünk
                domain = context.Request.Host.Host;
            }

            // --- ADATBÁZIS KERESÉS ---
            if (tenantId.HasValue)
            {
                // ID alapján keresünk
                var company = await db.Companies
                    .Include(c => c.Languages) // Nyelveket betöltjük előre
                    .FirstOrDefaultAsync(c => c.Id == tenantId.Value && !c.IsDeleted);

                if (company != null)
                {
                    tenantContext.CurrentCompany = company;
                }
            }
            else if (!string.IsNullOrEmpty(domain))
            {
                // Domain alapján keresünk
                // FIGYELEM: Localhost esetén óvatosnak kell lenni, fejlesztésnél a 'forceTenant' a javasolt.
                var company = await db.Companies
                    .Include(c => c.Languages)
                    .FirstOrDefaultAsync(c => c.Domain == domain && !c.IsDeleted);

                if (company != null)
                {
                    tenantContext.CurrentCompany = company;
                }
            }

            // Továbbengedjük a kérést a következő lépcsőre (Controller)
            await _next(context);
        }
    }
}