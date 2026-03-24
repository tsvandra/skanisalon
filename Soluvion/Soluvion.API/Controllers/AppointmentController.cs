using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
                return StatusCode(403, new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                // ÚJ: Kifejezetten 409 Conflict-ot küldünk egy fix belső hibakóddal!
                return StatusCode(409, new { errorCode = "OVERLAP", message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
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
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                // ÚJ: Szintén 409 Conflict!
                return StatusCode(409, new { errorCode = "OVERLAP", message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
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
                if (!success) return NotFound(new { message = "Időpont nem található." });

                return Ok(new { Message = "Időpont sikeresen törölve." });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Szerver hiba: {ex.Message}" });
            }
        }
    }
}