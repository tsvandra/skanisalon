using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Soluvion.API.Data;
using Soluvion.API.Models;
using Soluvion.API.Services; // ITenantContext

namespace Soluvion.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ServiceController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ITenantContext _tenantContext; // ÚJ

        public ServiceController(AppDbContext context, ITenantContext tenantContext)
        {
            _context = context;
            _tenantContext = tenantContext;
        }

        // Helper: Admin műveleteknél (POST/PUT) a User Claim a mérvadó
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
        // Most már nem kérünk paramétert, hanem a Contextből olvassuk ki
        [HttpGet]
        [AllowAnonymous] // Biztosítjuk, hogy bárki hívhassa
        public async Task<ActionResult<IEnumerable<Service>>> GetServices()
        {
            // 1. Megnézzük, melyik cég oldalán vagyunk (Tenant Context)
            var currentCompany = _tenantContext.CurrentCompany;

            if (currentCompany == null)
            {
                return BadRequest("Nem sikerült azonosítani a szalont (Tenant Context hiányzik).");
            }

            // 2. Szűrés a megtalált cégre
            return await _context.Services
                .Include(s => s.Variants)
                .Where(s => s.CompanyId == currentCompany.Id)
                .OrderBy(s => s.OrderIndex)
                .ToListAsync();
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<Service>> PostService(Service service)
        {
            int userCompanyId = GetUserCompanyId();
            if (userCompanyId == 0) return Unauthorized("Nincs érvényes CompanyId.");

            service.CompanyId = userCompanyId;

            if (service.Category == null || !service.Category.Any())
            {
                service.Category = new Dictionary<string, string> { { "hu", "Egyéb" } };
            }

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
            var existingService = await _context.Services
                .Include(s => s.Variants)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (existingService == null) return NotFound();
            if (existingService.CompanyId != userCompanyId) return Forbid();

            existingService.Name = service.Name;
            existingService.DefaultPrice = service.DefaultPrice;
            existingService.DefaultDuration = service.DefaultDuration;
            existingService.OrderIndex = service.OrderIndex;
            existingService.Category = service.Category;
            existingService.Description = service.Description;

            if (service.Variants == null) service.Variants = new List<ServiceVariant>();

            var incomingIds = service.Variants.Select(v => v.Id).ToList();
            var variantsToDelete = existingService.Variants
                .Where(v => v.Id != 0 && !incomingIds.Contains(v.Id))
                .ToList();

            foreach (var variant in variantsToDelete)
            {
                _context.ServiceVariants.Remove(variant);
            }

            foreach (var variant in service.Variants)
            {
                if (variant.Id == 0)
                {
                    existingService.Variants.Add(variant);
                }
                else
                {
                    var existingVariant = existingService.Variants.FirstOrDefault(v => v.Id == variant.Id);
                    if (existingVariant != null)
                    {
                        existingVariant.VariantName = variant.VariantName;
                        existingVariant.Price = variant.Price;
                        existingVariant.Duration = variant.Duration;
                    }
                }
            }

            try
            {
                await _context.SaveChangesAsync();
                return Ok(existingService);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Services.Any(e => e.Id == id)) return NotFound();
                else throw;
            }
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteService(int id)
        {
            var service = await _context.Services.FindAsync(id);
            if (service == null) return NotFound();

            int userCompanyId = GetUserCompanyId();
            if (service.CompanyId != userCompanyId) return Forbid();

            _context.Services.Remove(service);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}