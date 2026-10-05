using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Soluvion.API.DTOs;
using Soluvion.API.Interfaces;

namespace Soluvion.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class InventoryController : ControllerBase
    {
        private readonly IInventoryService _inventoryService;

        public InventoryController(IInventoryService inventoryService)
        {
            _inventoryService = inventoryService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<InventoryDocumentDto>>> GetAll()
        {
            var docs = await _inventoryService.GetDocumentsAsync();
            return Ok(docs);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<InventoryDocumentDto>> GetById(int id)
        {
            var doc = await _inventoryService.GetDocumentByIdAsync(id);
            if (doc == null) return NotFound();
            return Ok(doc);
        }

        [HttpPost]
        public async Task<ActionResult<InventoryDocumentDto>> Create([FromBody] CreateInventoryDocumentDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var (success, document, error) = await _inventoryService.CreateDocumentAsync(dto);
            
            if (!success) return BadRequest(new { Error = error });

            return CreatedAtAction(nameof(GetById), new { id = document!.Id }, document);
        }

        /// <summary>Egy foglalás napi zárásának visszavonása (sztornó).</summary>
        [HttpPost("appointment/{appointmentId:int}/reverse")]
        public async Task<IActionResult> ReverseAppointmentClosing(int appointmentId)
        {
            var (success, reversedDocuments, error) = await _inventoryService.ReverseAppointmentClosingAsync(appointmentId);

            if (!success) return BadRequest(new { Error = error });

            return Ok(new { ReversedDocuments = reversedDocuments });
        }
    }
}

