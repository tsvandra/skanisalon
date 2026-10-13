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
                var result = await _appointmentService.CreateAppointmentAsync(dto);
                return Ok(new { success = true, appointmentId = result.Id });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { errorCode = "FORBIDDEN", message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(409, new { errorCode = "OVERLAP", message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { errorCode = "BAD_REQUEST", message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { errorCode = "SERVER_ERROR", message = ex.Message, details = ex.InnerException?.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAppointments([FromQuery] DateTime start, [FromQuery] DateTime end, [FromQuery] int? employeeId)
        {
            try
            {
                var appointments = await _appointmentService.GetAppointmentsAsync(start, end, employeeId);
                return Ok(appointments);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { errorCode = "SERVER_ERROR", message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAppointment(int id, [FromBody] UpdateAppointmentDto dto)
        {
            try
            {
                var result = await _appointmentService.UpdateAppointmentAsync(id, dto);
                return Ok(new { success = true, appointmentId = result.Id });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { errorCode = "NOT_FOUND" });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { errorCode = "FORBIDDEN", message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(409, new { errorCode = "OVERLAP", message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { errorCode = "BAD_REQUEST", message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { errorCode = "SERVER_ERROR", message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAppointment(int id)
        {
            try
            {
                var success = await _appointmentService.DeleteAppointmentAsync(id);
                if (!success) return NotFound(new { errorCode = "NOT_FOUND" });

                return Ok(new { success = true });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { errorCode = "FORBIDDEN", message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { errorCode = "SERVER_ERROR", message = ex.Message });
            }
        }

        [HttpPost("{id}/mark-materials-recorded")]
        public async Task<IActionResult> MarkMaterialsRecorded(int id)
        {
            try
            {
                var context = HttpContext.RequestServices.GetRequiredService<Soluvion.API.Data.AppDbContext>();
                var app = await context.Appointments.FindAsync(id);
                if (app != null)
                {
                    app.MaterialUsageRecorded = true;
                    await context.SaveChangesAsync();
                }
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }
}