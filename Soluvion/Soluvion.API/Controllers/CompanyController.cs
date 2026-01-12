using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Soluvion.API.Data;
using Soluvion.API.Models;

namespace Soluvion.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CompanyController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CompanyController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Company>>> GetCompanies()
        {
            return await _context.Companies
                .Include(c => c.CompanyType)
                .ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Company>> GetCompanyById(int id)
        {
            var company = await _context.Companies
                .Include(c => c.CompanyType)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (company == null)
            {
                return NotFound();
            }

            return company;
        }

        [HttpPost]
        public async Task<ActionResult<Company>> CreateCompany(Company company)
        {
            _context.Companies.Add(company);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCompanyById), new { id = company.Id }, company);
        }
    }
}
