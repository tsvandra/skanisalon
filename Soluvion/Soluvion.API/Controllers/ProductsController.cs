using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Soluvion.API.DTOs;
using Soluvion.API.Interfaces;

namespace Soluvion.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;

        public ProductsController(IProductService productService)
        {
            _productService = productService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProductDto>>> GetAll()
        {
            var products = await _productService.GetAllProductsAsync();
            return Ok(products);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ProductDto>> GetById(int id)
        {
            var product = await _productService.GetProductByIdAsync(id);
            if (product == null) return NotFound();
            return Ok(product);
        }

        [HttpPost]
        public async Task<ActionResult<ProductDto>> Create([FromBody] CreateProductDto dto)
        {
            var product = await _productService.CreateProductAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateProductDto dto)
        {
            var success = await _productService.UpdateProductAsync(id, dto);
            if (!success) return NotFound();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _productService.DeleteProductAsync(id);
            if (!success) return NotFound();
            return NoContent();
        }

        [HttpPost("upload-image")]
        [RequestSizeLimit(15_000_000)]
        public async Task<ActionResult> UploadImage(
            IFormFile file,
            [FromServices] IImageService imageService,
            [FromServices] ITenantContext tenantContext)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "Nem található feltöltendő képfájl." });
            }

            int companyId = tenantContext.CurrentCompany?.Id ?? 0;
            if (companyId == 0)
            {
                return BadRequest(new { message = "Érvénytelen szalon azonosító." });
            }

            var uploadResult = await imageService.UploadImageAsync(file, $"soluvion/company_{companyId}/products", 1200);
            if (uploadResult == null)
            {
                return StatusCode(500, new { message = "Hiba a termékkép Cloudinary feltöltése során." });
            }

            return Ok(new { imageUrl = uploadResult.Value.Url });
        }

        [HttpPost("ai-scan")]
        [RequestSizeLimit(35_000_000)]
        public async Task<ActionResult<ProductAiScanResultDto>> AiScan(
            [FromForm] List<IFormFile> images,
            [FromForm] int? primaryImageIndex,
            [FromServices] IProductAiScannerService aiScannerService,
            [FromServices] IImageService imageService,
            [FromServices] ITenantContext tenantContext)
        {
            if (images == null || images.Count == 0)
            {
                return BadRequest(new { message = "Legalább 1 képet fel kell tölteni a termékről." });
            }

            try
            {
                var result = await aiScannerService.ScanProductImagesAsync(images);

                // Ha van kijelölt termékkép, töltsük fel a Cloudinary-ba a szalon saját mappájába
                if (primaryImageIndex.HasValue && primaryImageIndex.Value >= 0 && primaryImageIndex.Value < images.Count)
                {
                    int companyId = tenantContext.CurrentCompany?.Id ?? 0;
                    if (companyId > 0)
                    {
                        var primaryFile = images[primaryImageIndex.Value];
                        var uploadResult = await imageService.UploadImageAsync(primaryFile, $"soluvion/company_{companyId}/products", 1200);
                        if (uploadResult != null)
                        {
                            result.ImageUrl = uploadResult.Value.Url;
                        }
                    }
                }

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
                return StatusCode(500, new { message = $"Hiba a termék AI beolvasása során: {ex.Message}" });
            }
        }
    }
}

