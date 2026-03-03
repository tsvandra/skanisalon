using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Soluvion.API.Models.DTOs;
using Soluvion.API.Services;

namespace Soluvion.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GalleryController : ControllerBase
    {
        private readonly IGalleryService _galleryService;

        public GalleryController(IGalleryService galleryService)
        {
            _galleryService = galleryService;
        }

        // --- KÉPEK KEZELÉSE ---

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<GalleryImageDto>>> GetImages()
        {
            var result = await _galleryService.GetImagesAsync();
            return Ok(result);
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<GalleryImageDto>> UploadImage(IFormFile file, [FromForm] string category)
        {
            if (file == null || file.Length == 0) return BadRequest("Nem választottál ki képet!");

            var (imageDto, errorMessage) = await _galleryService.UploadImageAsync(file, category);

            if (errorMessage != null) return BadRequest(errorMessage);
            if (imageDto == null) return BadRequest();

            return Ok(imageDto);
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateImage(int id, [FromBody] GalleryImageUpdateDto dto)
        {
            if (id != dto.Id) return BadRequest();

            var success = await _galleryService.UpdateImageAsync(id, dto);
            if (!success) return NotFound();

            return Ok(new { message = "Sikeres mentés" });
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteImage(int id)
        {
            var success = await _galleryService.DeleteImageAsync(id);
            if (!success) return NotFound();

            return NoContent();
        }

        // --- KATEGÓRIÁK KEZELÉSE ---

        [HttpGet("categories")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<GalleryCategoryDto>>> GetCategories()
        {
            var result = await _galleryService.GetCategoriesAsync();
            return Ok(result);
        }

        [HttpPost("categories")]
        [Authorize]
        public async Task<ActionResult<GalleryCategoryDto>> CreateCategory([FromBody] CategoryDto dto)
        {
            var result = await _galleryService.CreateCategoryAsync(dto);
            if (result == null) return Unauthorized();

            return Ok(result);
        }

        [HttpPut("categories/{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] CategoryDto dto)
        {
            var success = await _galleryService.UpdateCategoryAsync(id, dto);
            if (!success) return NotFound();

            return Ok();
        }

        [HttpDelete("categories/{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var (success, errorMessage) = await _galleryService.DeleteCategoryAsync(id);

            if (!success && errorMessage == null) return NotFound(); // Nincs meg
            if (!success && errorMessage != null) return BadRequest(errorMessage); // Van benne kép

            return NoContent();
        }
    }
}