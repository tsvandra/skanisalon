using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Soluvion.API.Data;
using Soluvion.API.Models;
using Soluvion.API.Models.DTOs;
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

            var services = await _context.Services
                .Include(s => s.Variants)
                .Where(s => s.CompanyId == currentCompany.Id)
                .OrderBy(s => s.OrderIndex)
                .ToListAsync();

            var dtos = services.Select(MapToDto).ToList();
            return Ok(dtos);
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<ServiceDto>> PostService([FromBody] ServiceDto dto)
        {
            int userCompanyId = GetUserCompanyId();
            if (userCompanyId == 0) return Unauthorized();

            // Új Entity létrehozása DTO-ból
            var service = new Service
            {
                CompanyId = userCompanyId,
                Name = dto.Name ?? new Dictionary<string, string> { { "hu", "Új szolgáltatás" } },
                Category = dto.Category != null && dto.Category.Any() ? dto.Category : new Dictionary<string, string> { { "hu", "Egyéb" } },
                Description = dto.Description ?? new Dictionary<string, string>(),
                DefaultPrice = dto.DefaultPrice ?? 0,
                DefaultDuration = dto.DefaultDuration,
                OrderIndex = dto.OrderIndex,
                Variants = dto.Variants?.Select(v => new ServiceVariant
                {
                    VariantName = v.VariantName ?? new Dictionary<string, string> { { "hu", "Normál" } },
                    Price = v.Price ?? 0,
                    Duration = v.Duration
                }).ToList() ?? new List<ServiceVariant>()
            };

            _context.Services.Add(service);
            await _context.SaveChangesAsync();

            // Visszaadjuk az elmentett objektumot DTO-ként (immár a generált DB ID-kkal)
            return CreatedAtAction(nameof(GetServices), new { id = service.Id }, MapToDto(service));
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> PutService(int id, [FromBody] ServiceDto dto)
        {
            if (id != dto.Id) return BadRequest("Az ID nem egyezik.");
            int userCompanyId = GetUserCompanyId();

            var existing = await _context.Services
                .Include(s => s.Variants)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (existing == null || existing.CompanyId != userCompanyId) return NotFound();

            // 1. Alap adatok frissítése
            existing.Name = dto.Name;
            existing.Category = dto.Category;
            existing.Description = dto.Description;
            existing.DefaultPrice = dto.DefaultPrice ?? 0;
            existing.DefaultDuration = dto.DefaultDuration;
            existing.OrderIndex = dto.OrderIndex;

            // 2. Variánsok törlése (ami nincs benne az új listában, de az adatbázisban még ott van)
            var incomingIds = dto.Variants?.Select(v => v.Id).ToList() ?? new List<int>();
            var toDelete = existing.Variants.Where(v => v.Id != 0 && !incomingIds.Contains(v.Id)).ToList();
            _context.ServiceVariants.RemoveRange(toDelete);

            // 3. Variánsok hozzáadása / frissítése
            if (dto.Variants != null)
            {
                foreach (var vDto in dto.Variants)
                {
                    if (vDto.Id == 0)
                    {
                        // BIZTONSÁGOS LÉTREHOZÁS: Új példányt adunk az Entity Framework-nek
                        existing.Variants.Add(new ServiceVariant
                        {
                            VariantName = vDto.VariantName,
                            Price = vDto.Price ?? 0,
                            Duration = vDto.Duration
                        });
                    }
                    else
                    {
                        // Frissítés
                        var existVar = existing.Variants.FirstOrDefault(x => x.Id == vDto.Id);
                        if (existVar != null)
                        {
                            existVar.VariantName = vDto.VariantName;
                            existVar.Price = vDto.Price ?? 0;
                            existVar.Duration = vDto.Duration;
                        }
                    }
                }
            }

            await _context.SaveChangesAsync();

            // Visszaküldjük a frissített állapotot DTO-ként a Frontennek
            return Ok(MapToDto(existing));
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

        // --- SEGÉDMETÓDUS ---
        private ServiceDto MapToDto(Service s)
        {
            return new ServiceDto
            {
                Id = s.Id,
                CompanyId = s.CompanyId,
                Name = s.Name,
                Category = s.Category,
                Description = s.Description,
                DefaultPrice = s.DefaultPrice,
                DefaultDuration = s.DefaultDuration,
                OrderIndex = s.OrderIndex,
                Variants = s.Variants?.Select(v => new ServiceVariantDto
                {
                    Id = v.Id,
                    VariantName = v.VariantName,
                    Price = v.Price,
                    Duration = v.Duration
                }).ToList() ?? new List<ServiceVariantDto>()
            };
        }
    }
}