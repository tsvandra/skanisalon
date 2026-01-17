using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Soluvion.API.Data;
using Soluvion.API.Models;

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

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Service>>> GetServices(int companyId)
        {
            if (companyId <= 0)
            {
                return BadRequest("CompanyId megadasa kotelezo.");
            }

            var services = await _context.Services
                .Where(s => s.CompanyId == companyId)
                .ToListAsync();

            return Ok(services);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Service>> GetService(int id)
        {
            var service = await _context.Services.FindAsync(id);

            if (service == null) 
            {
                return NotFound();
            }
            return service;
        }

        [HttpPost]
        public async Task<ActionResult<Service>> PostService(Service service)
        {
            var companyExists = await _context.Companies.AnyAsync(c => c.Id == service.CompanyId);
            if (!companyExists)
            {
                return BadRequest($"Nem letezik ceg ezzel az Id-val: {service.CompanyId}");
            }

            _context.Services.Add(service);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetService), new { id = service.Id }, service);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteServie(int id)
        {
            var service = await _context.Services.FindAsync(id);
            if (service == null)
            {
                return NotFound();
            }

            _context.Services.Remove(service);
            await _context.SaveChangesAsync();

            return NoContent();
        }


    }
}
