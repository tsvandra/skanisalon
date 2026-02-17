using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Soluvion.API.Data;
using Soluvion.API.Models;
using Soluvion.API.Models.DTOs; // ServiceDto
using Soluvion.API.Services;

namespace Soluvion.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ServiceController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ITenantContext _tenantContext;

        public ServiceController(AppDbContext context, ITenantContext tenantContext)
        {
            _context = context;
            _tenantContext = tenantContext;
        }

        private int GetUserCompanyId()
        {
            var companyClaim = User.FindFirst("CompanyId");
            if (companyClaim != null && int.TryParse(companyClaim.Value, out int companyId))
            {
                return companyId;
            }
            return 0;
        }

        // GET: api/Service (Publikus lista)
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<ServiceDto>>> GetServices()
        {
            var currentCompany = _tenantContext.CurrentCompany;
            if (currentCompany == null) return BadRequest("Tenant Context hiányzik.");

            // Adatbázisból lekérés
            var services = await _context.Services
                .Include(s => s.Variants)
                .Where(s => s.CompanyId == currentCompany.Id)
                .OrderBy(s => s.OrderIndex)
                .ToListAsync();

            // Mapping Entity -> DTO (Kézi konverzió a biztonságért)
            var dtos = services.Select(s => new ServiceDto
            {
                Id = s.Id,
                CompanyId = s.CompanyId,
                Name = s.Name,
                Category = s.Category,
                Description = s.Description,
                DefaultPrice = s.DefaultPrice,
                DefaultDuration = s.DefaultDuration,
                OrderIndex = s.OrderIndex,
                Variants = s.Variants.Select(v => new ServiceVariantDto
                {
                    Id = v.Id,
                    VariantName = v.VariantName,
                    Price = v.Price,
                    Duration = v.Duration
                }).ToList()
            }).ToList();

            return Ok(dtos);
        }

        // POST, PUT, DELETE metódusoknál is érdemes DTO-t fogadni, 
        // de a mostani hiba szempontjából a GET a kritikus. 
        // A te meglévő POST/PUT kódod működhet, ha a kliens jó JSON-t küld,
        // de itt hagyom a biztonságos Entity verziót, amit már megírtunk:

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<Service>> PostService(Service service)
        {
            int userCompanyId = GetUserCompanyId();
            if (userCompanyId == 0) return Unauthorized();

            service.CompanyId = userCompanyId;
            if (service.Category == null || !service.Category.Any())
                service.Category = new Dictionary<string, string> { { "hu", "Egyéb" } };

            _context.Services.Add(service);
            await _context.SaveChangesAsync();
            return CreatedAtAction("GetServices", new { id = service.Id }, service);
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> PutService(int id, Service service)
        {
            if (id != service.Id) return BadRequest();
            int userCompanyId = GetUserCompanyId();

            var existing = await _context.Services
                .Include(s => s.Variants)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (existing == null || existing.CompanyId != userCompanyId) return NotFound();

            // Update logic
            existing.Name = service.Name;
            existing.Category = service.Category;
            existing.Description = service.Description;
            existing.DefaultPrice = service.DefaultPrice;
            existing.DefaultDuration = service.DefaultDuration;
            existing.OrderIndex = service.OrderIndex;

            // Variants merge logic (meglévő kódod alapján)
            var incomingIds = service.Variants.Select(v => v.Id).ToList();
            var toDelete = existing.Variants.Where(v => v.Id != 0 && !incomingIds.Contains(v.Id)).ToList();
            _context.ServiceVariants.RemoveRange(toDelete);

            foreach (var v in service.Variants)
            {
                if (v.Id == 0) existing.Variants.Add(v);
                else
                {
                    var existVar = existing.Variants.FirstOrDefault(x => x.Id == v.Id);
                    if (existVar != null)
                    {
                        existVar.VariantName = v.VariantName;
                        existVar.Price = v.Price;
                        existVar.Duration = v.Duration;
                    }
                }
            }

            await _context.SaveChangesAsync();
            return Ok(existing);
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteService(int id)
        {
            var s = await _context.Services.FindAsync(id);
            if (s == null) return NotFound();
            if (s.CompanyId != GetUserCompanyId()) return Forbid();

            _context.Services.Remove(s);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}