using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Soluvion.API.Data;
using Soluvion.API.DTOs.AppointmentDtos;
using Soluvion.API.Interfaces;
using Soluvion.Domain.Models;
using Soluvion.Domain.Models.Enums;

namespace Soluvion.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous] // Bárki elérheti!
    public class PublicBookingController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ISmartBookingEngine _bookingEngine;
        private readonly ITenantContext _tenantContext;

        public PublicBookingController(AppDbContext context, ISmartBookingEngine bookingEngine, ITenantContext tenantContext)
        {
            _context = context;
            _bookingEngine = bookingEngine;
            _tenantContext = tenantContext;
        }

        [HttpPost]
        public async Task<IActionResult> CreateGuestBooking([FromBody] GuestBookingDto dto)
        {
            var company = _tenantContext.CurrentCompany;
            if (company == null)
            {
                return NotFound(new { errorCode = "COMPANY_NOT_FOUND" });
            }

            if (!company.IsOnlineBookingEnabled)
            {
                return StatusCode(403, new { errorCode = "ONLINE_BOOKING_DISABLED" });
            }

            int companyId = company.Id;

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Biztonságos Dolgozó (Employee) keresés
                var employeeExists = await _context.CompanyEmployees.AnyAsync(e => e.CompanyId == companyId && e.Id == dto.EmployeeId && e.IsActive);
                if (!employeeExists)
                {
                    var defaultEmployee = await _context.CompanyEmployees.FirstOrDefaultAsync(e => e.CompanyId == companyId && e.IsActive);
                    if (defaultEmployee == null)
                    {
                        return BadRequest(new { errorCode = "NO_ACTIVE_EMPLOYEE" });
                    }

                    dto.EmployeeId = defaultEmployee.Id;
                }

                // 2. Biztonságos JSONB Customer keresés
                var allCustomers = await _context.CompanyCustomers.Where(c => c.CompanyId == companyId).ToListAsync();
                var customer = allCustomers.FirstOrDefault(c => c.Attributes != null &&
                                                                c.Attributes.ContainsKey("Email") &&
                                                                c.Attributes["Email"] == dto.Email);

                if (customer == null)
                {
                    customer = new CompanyCustomer
                    {
                        CompanyId = companyId,
                        Attributes = dto.Attributes ?? new Dictionary<string, string>()
                    };
                    customer.Attributes["Email"] = dto.Email;
                    customer.Attributes["Phone"] = dto.Phone;
                    customer.Attributes["FullName"] = dto.FullName;

                    _context.CompanyCustomers.Add(customer);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    if (dto.Attributes != null)
                    {
                        foreach (var attr in dto.Attributes)
                        {
                            customer.Attributes[attr.Key] = attr.Value;
                        }
                    }
                    _context.CompanyCustomers.Update(customer);
                }

                // 3. Smart Booking Engine hívása
                var (duration, price) = await _bookingEngine.CalculateAppointmentDetailsAsync(customer.Id, dto.ServiceVariantIds);
                DateTime endDateTime = dto.StartDateTime.AddMinutes(duration);

                bool isAvailable = await _bookingEngine.IsTimeSlotAvailableAsync(companyId, dto.EmployeeId, dto.StartDateTime, endDateTime, force: false);
                if (!isAvailable)
                {
                    return Conflict(new { errorCode = "BOOKING_SLOT_TAKEN" });
                }

                // 4. Appointment létrehozása
                var appointment = new Appointment
                {
                    CompanyId = companyId,
                    CustomerId = customer.Id,
                    EmployeeId = dto.EmployeeId,
                    StartDateTime = dto.StartDateTime,
                    EndDateTime = endDateTime,
                    TotalPrice = price,
                    Status = AppointmentStatus.Pending,
                    Source = BookingSource.Web,
                    CustomerNotes = dto.Notes
                };

                foreach (var variantId in dto.ServiceVariantIds)
                {
                    var variant = await _context.ServiceVariants.FindAsync(variantId);
                    if (variant != null)
                    {
                        appointment.Items.Add(new AppointmentItem
                        {
                            ServiceVariantId = variantId,
                            Price = variant.Price,
                            CalculatedDurationMinutes = variant.Duration
                        });
                    }
                }

                _context.Appointments.Add(appointment);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { success = true, appointmentId = appointment.Id });
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { errorCode = "SERVER_ERROR" });
            }
        }
    }
}