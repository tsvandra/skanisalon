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
                return Forbid(ex.Message); // 403-as hiba jogosulatlan művelet esetén
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message); // 400-as hiba ütközés esetén
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAppointments([FromQuery] DateTime start, [FromQuery] DateTime end, [FromQuery] int? employeeId)
        {
            string username = User.FindFirstValue(ClaimTypes.Name)!;
            var appointments = await _appointmentService.GetAppointmentsAsync(start, end, username, employeeId);
            return Ok(appointments);
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
            catch (Exception ex) when (ex is UnauthorizedAccessException || ex is InvalidOperationException)
            {
                return BadRequest(ex.Message);
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
        }
    }
}