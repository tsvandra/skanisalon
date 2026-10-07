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

        /// <summary>Szállítólevél (dodací list) képek AI elemzése.</summary>
        [HttpPost("scan-delivery-note")]
        [RequestSizeLimit(35_000_000)]
        public async Task<ActionResult<DeliveryNoteScanResultDto>> ScanDeliveryNote(
            [FromForm] List<IFormFile> images,
            [FromServices] IProductAiScannerService aiScannerService)
        {
            if (images == null || images.Count == 0)
            {
                return BadRequest(new { message = "Legalább 1 képet fel kell tölteni a szállítólevélről." });
            }

            try
            {
                var result = await aiScannerService.ScanDeliveryNoteImagesAsync(images);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Hiba a szállítólevél AI elemzése során: {ex.Message}" });
            }
        }

        /// <summary>Szállítólevél alapján új termékek létrehozása és készlet bevételezése.</summary>
        [HttpPost("import-delivery-note")]
        public async Task<ActionResult<ImportDeliveryNoteResultDto>> ImportDeliveryNote([FromBody] ImportDeliveryNoteDto dto)
        {
            var (success, result, error) = await _inventoryService.ImportDeliveryNoteAsync(dto);

            if (!success)
            {
                return BadRequest(new { message = error });
            }

            return Ok(result);
        }
    }
}

