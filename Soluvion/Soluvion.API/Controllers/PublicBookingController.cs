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
            int companyId = _tenantContext.CurrentCompany?.Id ?? throw new Exception("Nincs kiválasztva cég.");

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // ÚJ LOGIKA 1: Biztonságos Dolgozó (Employee) keresés
                // Ha a frontend által küldött (pl. 1-es) ID nem létezik, automatikusan az első aktív dolgozóhoz rendeljük
                var employeeExists = await _context.CompanyEmployees.AnyAsync(e => e.CompanyId == companyId && e.Id == dto.EmployeeId);
                if (!employeeExists)
                {
                    var defaultEmployee = await _context.CompanyEmployees.FirstOrDefaultAsync(e => e.CompanyId == companyId && e.IsActive);
                    if (defaultEmployee == null) throw new Exception("Nincs aktív dolgozó ebben a cégben, akihez foglalni lehetne!");

                    dto.EmployeeId = defaultEmployee.Id; // Kicseréljük a valós ID-ra
                }

                // ÚJ LOGIKA 2: Biztonságos JSONB Customer keresés
                // Lekérjük a cég összes vendégét, és a memóriában (C#-ban) szűrünk, hogy elkerüljük a PostgreSQL fordítási hibákat
                var allCustomers = await _context.CompanyCustomers.Where(c => c.CompanyId == companyId).ToListAsync();
                var customer = allCustomers.FirstOrDefault(c => c.Attributes != null &&
                                                                c.Attributes.ContainsKey("Email") &&
                                                                c.Attributes["Email"] == dto.Email);

                if (customer == null)
                {
                    customer = new CompanyCustomer
                    {
                        CompanyId = companyId,
                        Attributes = dto.Attributes
                    };
                    customer.Attributes["Email"] = dto.Email;
                    customer.Attributes["Phone"] = dto.Phone;
                    customer.Attributes["FullName"] = dto.FullName;

                    _context.CompanyCustomers.Add(customer);
                    await _context.SaveChangesAsync(); // Hogy megkapja a CustomerId-t
                }
                else
                {
                    foreach (var attr in dto.Attributes)
                    {
                        customer.Attributes[attr.Key] = attr.Value;
                    }
                    _context.CompanyCustomers.Update(customer);
                }

                // 2. Smart Booking Engine hívása
                var (duration, price) = await _bookingEngine.CalculateAppointmentDetailsAsync(customer.Id, dto.ServiceVariantIds);
                DateTime endDateTime = dto.StartDateTime.AddMinutes(duration);

                bool isAvailable = await _bookingEngine.IsTimeSlotAvailableAsync(companyId, dto.EmployeeId, dto.StartDateTime, endDateTime, force: false);
                if (!isAvailable)
                {
                    return BadRequest("Sajnos ez az időpont már foglalt. Kérlek, válassz másikat!");
                }

                // 3. Appointment létrehozása
                var appointment = new Appointment
                {
                    CompanyId = companyId,
                    CustomerId = customer.Id,
                    EmployeeId = dto.EmployeeId,
                    StartDateTime = dto.StartDateTime,
                    EndDateTime = endDateTime,
                    TotalPrice = price,
                    Status = AppointmentStatus.Pending,
                    Notes = dto.Notes
                };

                foreach (var variantId in dto.ServiceVariantIds)
                {
                    var variant = await _context.ServiceVariants.FindAsync(variantId);
                    appointment.Items.Add(new AppointmentItem
                    {
                        ServiceVariantId = variantId,
                        Price = variant!.Price,
                        CalculatedDurationMinutes = variant.Duration
                    });
                }

                _context.Appointments.Add(appointment);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { Message = "Sikeres foglalás", AppointmentId = appointment.Id });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                // ÚJ LOGIKA 3: Pontos hibaüzenet visszaadása a frontendnek
                return StatusCode(500, $"Belső szerverhiba: {ex.Message} {(ex.InnerException != null ? " - Részletek: " + ex.InnerException.Message : "")}");
            }
        }
    }
}