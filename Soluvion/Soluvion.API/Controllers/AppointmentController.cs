using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Soluvion.API.DTOs;
using Soluvion.API.DTOs.AppointmentDtos;
using Soluvion.API.Interfaces;
using System.Security.Claims;

namespace Soluvion.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AppointmentController : ControllerBase
    {
        private readonly IAppointmentService _appointmentService;

        public AppointmentController(IAppointmentService appointmentService)
        {
            _appointmentService = appointmentService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateAppointment([FromBody] CreateAppointmentDto dto)
        {
            try
            {
                string username = User.FindFirstValue(ClaimTypes.Name)!;
                var result = await _appointmentService.CreateAppointmentAsync(dto, username);
                return Ok(new { Message = "Sikeres foglalás", AppointmentId = result.Id });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message); // 403
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message); // 400 (Pl. ütközés)
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message); // 400 (Pl. rossz variáns ID)
            }
            catch (Exception ex)
            {
                // VÉDELEM: Ha bármi más elszáll, küldjük vissza a frontendnek a pontos hibaüzenetet!
                return StatusCode(500, new { message = $"Szerver hiba: {ex.Message}", details = ex.InnerException?.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAppointments([FromQuery] DateTime start, [FromQuery] DateTime end, [FromQuery] int? employeeId)
        {
            try
            {
                string username = User.FindFirstValue(ClaimTypes.Name)!;
                var appointments = await _appointmentService.GetAppointmentsAsync(start, end, username, employeeId);
                return Ok(appointments);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Szerver hiba a letöltéskor: {ex.Message}" });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAppointment(int id, [FromBody] UpdateAppointmentDto dto)
        {
            try
            {
                string username = User.FindFirstValue(ClaimTypes.Name)!;
                var result = await _appointmentService.UpdateAppointmentAsync(id, dto, username);
                return Ok(new { Message = "Sikeres frissítés", AppointmentId = result.Id });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException || ex is InvalidOperationException || ex is ArgumentException)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Szerver hiba: {ex.Message}" });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAppointment(int id)
        {
            try
            {
                string username = User.FindFirstValue(ClaimTypes.Name)!;
                var success = await _appointmentService.DeleteAppointmentAsync(id, username);
                if (!success) return NotFound("Időpont nem található.");

                return Ok(new { Message = "Időpont sikeresen törölve." });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Szerver hiba: {ex.Message}" });
            }
        }
    }
}