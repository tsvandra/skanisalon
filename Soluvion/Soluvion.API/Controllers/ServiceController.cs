using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Soluvion.API.Data;
using Soluvion.API.Models;
using System.Security.Claims;

namespace Soluvion.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ServiceController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ServiceController(AppDbContext context)
        {
            _context = context;
        }

        private int GetCurrentCompanyId()
        {
            var companyClaim = User.FindFirst("CompanyId");
            if (companyClaim != null && int.TryParse(companyClaim.Value, out int companyId))
            {
                return companyId;
            }
            return 0;
        }

        // GET: api/Service
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Service>>> GetServices(int companyId)
        {
            if (companyId <= 0) return BadRequest("CompanyId megadása kötelező.");

            return await _context.Services
                .Include(s => s.Variants)
                .Where(s => s.CompanyId == companyId)
                .OrderBy(s => s.OrderIndex)
                .ToListAsync();
        }

        // POST: api/Service
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<Service>> PostService(Service service)
        {
            int userCompanyId = GetCurrentCompanyId();
            if (userCompanyId == 0) return Unauthorized("Nincs érvényes CompanyId.");

            service.CompanyId = userCompanyId;

            // Ha nincs kategória, legyen alapértelmezett
            if (string.IsNullOrEmpty(service.Category)) service.Category = "Egyéb";

            _context.Services.Add(service);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetServices", new { id = service.Id }, service);
        }

        // PUT: api/Service/5
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> PutService(int id, Service service)
        {
            if (id != service.Id) return BadRequest();

            int userCompanyId = GetCurrentCompanyId();
            var existingService = await _context.Services
                .Include(s => s.Variants)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (existingService == null) return NotFound();
            if (existingService.CompanyId != userCompanyId) return Forbid();

            // --- MEZŐK FRISSÍTÉSE (ITT VOLT A HIBA) ---
            existingService.Name = service.Name;
            existingService.DefaultPrice = service.DefaultPrice;
            existingService.DefaultDuration = service.DefaultDuration;
            existingService.OrderIndex = service.OrderIndex;

            // JAVÍTÁS: A Category mezőt kifejezetten frissítjük!
            existingService.Category = service.Category;

            // Variánsok kezelése (meglévő logika)
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

        // DELETE: api/Service/5
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteService(int id)
        {
            var service = await _context.Services.FindAsync(id);
            if (service == null) return NotFound();

            int userCompanyId = GetCurrentCompanyId();
            if (service.CompanyId != userCompanyId) return Forbid();

            _context.Services.Remove(service);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}