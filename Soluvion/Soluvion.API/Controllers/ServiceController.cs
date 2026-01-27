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

        // GET: api/Service?companyId=1
        // Bárki láthatja az árakat
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Service>>> GetServices(int companyId)
        {
            if (companyId <= 0) return BadRequest("CompanyId megadása kötelező.");

            return await _context.Services
                .Where(s => s.CompanyId == companyId)
                .ToListAsync();
        }

        // POST: api/Service
        // Csak Admin vehet fel új árat!
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<Service>> PostService(Service service)
        {
            // 1. Megnézzük, ki van bejelentkezve (Tokenből CompanyId)
            int companyId = 1; // Alapértelmezett biztonsági háló
            var companyClaim = User.FindFirst("CompanyId");
            if (companyClaim != null) int.TryParse(companyClaim.Value, out companyId);

            // 2. Felülírjuk a beküldött adatot a biztosra (hogy ne lehessen más cég nevében írni)
            service.CompanyId = companyId;

            // 3. Mentés
            _context.Services.Add(service);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetServices), new { companyId = service.CompanyId }, service);
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
            var companyClaim = User.FindFirst("CompanyId");
            if (companyClaim != null)
            {
                int userCompanyId = int.Parse(companyClaim.Value);
                if (service.CompanyId != userCompanyId)
                {
                    return Forbid("Nincs jogod más cég szolgáltatását törölni!");
                }
            }

            _context.Services.Remove(service);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}