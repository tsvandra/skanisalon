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
            var compnayClaim = User.FindFirst("CompanyId");
            if (compnayClaim != null && int.TryParse(compnayClaim.Value, out int companyId))
            {
                return companyId;
            }
            return 0;
        }

        // GET: api/Service?companyId=1
        // Bárki láthatja az árakat
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
        // Csak Admin vehet fel új árat!
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<Service>> PostService(Service service)
        {
            int companyId = GetCurrentCompanyId();
            if (companyId == 0) return Unauthorized("Érvénytelen token vagy hiányzó cégadatok.");

            // 2. Felülírjuk a beküldött adatot a biztosra (hogy ne lehessen más cég nevében írni)
            service.CompanyId = companyId;

            if (service.OrderIndex == 0)
            {
                var maxIndex = await _context.Services
                    .Where(s => s.CompanyId == companyId)
                    .MaxAsync(s => (int?)s.OrderIndex) ?? 0;
                service.OrderIndex = maxIndex + 1;
            }

            if (service.Variants != null)
            {
                foreach (var variant in service.Variants)
                {
                    variant.Id = 0;
                }
            }

            // 3. Mentés
            _context.Services.Add(service);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetServices), new { companyId = service.CompanyId }, service);
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> PutService(int id, Service service)
        {
            if (id != service.Id) return BadRequest("Az ID nem egyezik.");

            int userCompanyId = GetCurrentCompanyId();
            if (userCompanyId == 0) return Unauthorized();

            var existingService = await _context.Services
                .Include(s => s.Variants)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (existingService == null) return NotFound();

            if (existingService.CompanyId != userCompanyId)
            {
                return Forbid();
            }

            existingService.Name = service.Name;
            existingService.DefaultPrice = service.DefaultPrice;
            existingService.DefaultDuration = service.DefaultDuration;
            existingService.PictogramLink = service.PictogramLink;

            foreach (var incomingVariant in service.Variants)
            {
                if (incomingVariant.Id == 0)
                {
                    existingService.Variants.Add(incomingVariant);
                }
                else
                {
                    var existingVariant = existingService.Variants
                        .FirstOrDefault(v => v.Id == incomingVariant.Id);

                    if (existingVariant != null)
                    {
                        existingVariant.VariantName = incomingVariant.VariantName;
                        existingVariant.Price = incomingVariant.Price;
                        existingVariant.Duration = incomingVariant.Duration;
                    }
                }
            }

            var incomingIds = service.Variants.Select(v  => v.Id).ToList();

            var variantsToDelete = existingService.Variants
                .Where(v  => v.Id != 0  && !incomingIds.Contains(v.Id))
                .ToList();

            foreach (var variant in variantsToDelete)
            {
                _context.ServiceVariants.Remove(variant);
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Services.Any(e => e.Id == id)) return NotFound();
                else throw;
            }

            return NoContent();
        }
        // DELETE: api/Service/5
        // Csak Admin törölhet!
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteService(int id)
        {
            var service = await _context.Services.FindAsync(id);
            if (service == null) return NotFound();

            // Biztonsági ellenőrzés: A törölni kívánt szolgáltatás ehhez a céghez tartozik?
            int userCompanyId = GetCurrentCompanyId();

            if (service.CompanyId != userCompanyId)
            {
                return Forbid("Nincs jogod más cég szolgáltatását törölni!");
            }

            _context.Services.Remove(service);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}