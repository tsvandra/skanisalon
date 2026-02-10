using Microsoft.AspNetCore.Mvc;
using Soluvion.API.Services;

namespace Soluvion.API.Controllers
{
    // DTO (Data Transfer Object) a kéréshez: mit és mire fordítsunk
    public class TranslationRequest
    {
        public string Text { get; set; } = string.Empty;
        public string TargetLanguage { get; set; } = "en"; // Alapértelmezett: angol
    }

    [Route("api/[controller]")]
    [ApiController]
    public class TranslationController : ControllerBase
    {
        private readonly ITranslationService _translationService;

        public TranslationController(ITranslationService translationService)
        {
            _translationService = translationService;
        }

        // POST: api/Translation
        // Példa hívás: { "text": "Hajvágás", "targetLanguage": "en" }
        [HttpPost]
        public async Task<IActionResult> Translate([FromBody] TranslationRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Text))
            {
                return BadRequest("Nincs szöveg, amit lefordíthatnék.");
            }

            // Meghívjuk a service-t (ami az OpenAI-t hívja)
            var translatedText = await _translationService.TranslateTextAsync(request.Text, request.TargetLanguage);

            // Visszaadjuk a frontendnek JSON-ben
            return Ok(new { translatedText });
        }
    }
}