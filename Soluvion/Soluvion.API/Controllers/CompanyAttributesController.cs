using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Soluvion.API.DTOs.CompanyAttributeDtos;
using Soluvion.API.Interfaces;

namespace Soluvion.API.Controllers
{
    [ApiController]
    [Route("api/company-attributes")]
    [Authorize] // Admin jogosultságokat a Te projekted szerinti Policy-vel kiegészítheted
    public class CompanyAttributesController : ControllerBase
    {
        private readonly ICompanyAttributeService _service;

        public CompanyAttributesController(ICompanyAttributeService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<List<CompanyAttributeDto>>> Get()
        {
            return Ok(await _service.GetAttributesAsync());
        }

        [HttpPost]
        public async Task<ActionResult<CompanyAttributeDto>> Create([FromBody] CreateUpdateCompanyAttributeDto dto)
        {
            var result = await _service.CreateAttributeAsync(dto);
            return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<CompanyAttributeDto>> Update(int id, [FromBody] CreateUpdateCompanyAttributeDto dto)
        {
            return Ok(await _service.UpdateAttributeAsync(id, dto));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _service.DeleteAttributeAsync(id);
            return NoContent();
        }
    }
}