using Microsoft.EntityFrameworkCore;
using Soluvion.API.Data;
using Soluvion.API.DTOs.AppointmentDtos;
using Soluvion.API.Interfaces;
using Soluvion.Domain.Models;
using Soluvion.Domain.Models.Enums;

namespace Soluvion.API.Services
{
    public class AppointmentService : IAppointmentService
    {
        private readonly AppDbContext _context;
        private readonly ISmartBookingEngine _bookingEngine;
        private readonly ITenantContext _tenantContext;

        public AppointmentService(AppDbContext context, ISmartBookingEngine bookingEngine, ITenantContext tenantContext)
        {
            _context = context;
            _bookingEngine = bookingEngine;
            _tenantContext = tenantContext;
        }

        public async Task<List<AppointmentResponseDto>> GetAppointmentsAsync(DateTime start, DateTime end, string username, int? employeeId = null)
        {
            int companyId = _tenantContext.CurrentCompany?.Id ?? throw new Exception("Nincs kiválasztva cég.");

            var currentUser = await _context.Users.SingleAsync(u => u.Username == username);
            var currentEmployee = await _context.CompanyEmployees
                .SingleOrDefaultAsync(e => e.UserId == currentUser.Id && e.CompanyId == companyId);

            if (currentEmployee == null) throw new UnauthorizedAccessException("Nem vagy hozzárendelve ehhez a céghez.");

            var query = _context.Appointments
                .Include(a => a.Items)
                .Where(a => a.CompanyId == companyId && a.StartDateTime >= start && a.StartDateTime <= end);

            if (currentEmployee.Role == EmployeeRole.Worker)
            {
                query = query.Where(a => a.EmployeeId == currentEmployee.Id);
            }
            else if (employeeId.HasValue)
            {
                query = query.Where(a => a.EmployeeId == employeeId.Value);
            }

            var appointments = await query.Select(a => new AppointmentResponseDto
            {
                Id = a.Id,
                CustomerId = a.CustomerId,
                EmployeeId = a.EmployeeId,
                StartDateTime = a.StartDateTime,
                EndDateTime = a.EndDateTime,
                TotalPrice = a.TotalPrice,
                Status = a.Status.ToString(),
                Notes = (a.CustomerNotes != null && a.CustomerNotes != "") ? a.CustomerNotes : a.AdminNotes,

                Items = a.Items.Select(i => new AppointmentItemResponseDto
                {
                    Id = i.Id,
                    ServiceVariantId = i.ServiceVariantId,
                    CalculatedDurationMinutes = i.CalculatedDurationMinutes,
                    Price = i.Price
                }).ToList()
            }).ToListAsync();

            return appointments;
        }

        public async Task<Appointment> CreateAppointmentAsync(CreateAppointmentDto dto, string username)
        {
            int companyId = _tenantContext.CurrentCompany?.Id ?? throw new Exception("Nincs kiválasztva cég.");
            var company = await _context.Companies.FindAsync(companyId);

            // JAVÍTÁS: MVP tesztidőszak miatt ideiglenesen engedélyezzük a Free csomagnak is!
            /* if (company!.SubscriptionPlan == SubscriptionPlan.Free)
            {
                throw new InvalidOperationException("Az okos időpontfoglaló modul használatához legalább Basic előfizetés szükséges.");
            }
            */

            var currentUser = await _context.Users.SingleAsync(u => u.Username == username);
            var currentEmployee = await _context.CompanyEmployees
                .SingleOrDefaultAsync(e => e.UserId == currentUser.Id && e.CompanyId == companyId);

            if (currentEmployee == null)
                throw new UnauthorizedAccessException("Nem vagy hozzárendelve ehhez a céghez.");

            var targetEmployeeExists = await _context.CompanyEmployees.AnyAsync(e => e.Id == dto.EmployeeId && e.CompanyId == companyId);
            if (!targetEmployeeExists)
            {
                dto.EmployeeId = currentEmployee.Id;
            }

            if (dto.Force && currentEmployee.Role != EmployeeRole.Owner && currentEmployee.Role != EmployeeRole.Manager)
            {
                throw new UnauthorizedAccessException("Ütköző időpontot csak a Tulajdonos vagy a Menedzser erőszakolhat ki (Force).");
            }

            var variantIds = dto.Items.Select(i => i.ServiceVariantId).ToList();
            var (_, price) = await _bookingEngine.CalculateAppointmentDetailsAsync(dto.CustomerId, variantIds);

            int totalDuration = dto.Items.Sum(i => i.DurationMinutes);
            DateTime endDateTime = dto.StartDateTime.AddMinutes(totalDuration);

            bool isAvailable = await _bookingEngine.IsTimeSlotAvailableAsync(companyId, dto.EmployeeId, dto.StartDateTime, endDateTime, dto.Force);
            if (!isAvailable)
            {
                throw new InvalidOperationException("A kiválasztott időpont ütközik egy másikkal."); // <-- Ha itt 500-at kapsz, az emiatt van (ütközés)!
            }

            var appointment = new Appointment
            {
                CompanyId = companyId,
                CustomerId = dto.CustomerId,
                EmployeeId = dto.EmployeeId,
                StartDateTime = dto.StartDateTime,
                EndDateTime = endDateTime,
                TotalPrice = price,
                Status = dto.Status,
                Source = BookingSource.System,
                AdminNotes = dto.Notes,
                Items = new List<AppointmentItem>()
            };

            foreach (var itemDto in dto.Items)
            {
                var variant = await _context.ServiceVariants.FindAsync(itemDto.ServiceVariantId);
                if (variant == null) throw new ArgumentException($"Hiba: A(z) {itemDto.ServiceVariantId} variáns nem található!");

                appointment.Items.Add(new AppointmentItem
                {
                    ServiceVariantId = itemDto.ServiceVariantId,
                    Price = variant.Price,
                    CalculatedDurationMinutes = itemDto.DurationMinutes
                });
            }

            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();

            return appointment;
        }

        public async Task<Appointment> UpdateAppointmentAsync(int appointmentId, UpdateAppointmentDto dto, string username)
        {
            // ... A kód ezen része változatlan, megtartva a meglévő logikádat ...
            int companyId = _tenantContext.CurrentCompany?.Id ?? throw new Exception("Nincs kiválasztva cég.");

            var appointment = await _context.Appointments.Include(a => a.Items)
                                            .FirstOrDefaultAsync(a => a.Id == appointmentId && a.CompanyId == companyId);

            if (appointment == null) throw new KeyNotFoundException("Időpont nem található vagy nincs jogosultságod.");

            var currentUser = await _context.Users.SingleAsync(u => u.Username == username);
            var currentEmployee = await _context.CompanyEmployees
            .SingleOrDefaultAsync(e => e.UserId == currentUser.Id && e.CompanyId == companyId);

            // JAVÍTÁS: Ezek eddig InvalidOperationException-ök voltak, mostantól UnauthorizedAccessException!
            if (currentEmployee == null)
                throw new UnauthorizedAccessException("Nincs jogosultságod.");

            if (dto.Force && currentEmployee.Role != EmployeeRole.Owner && currentEmployee.Role != EmployeeRole.Manager)
                throw new UnauthorizedAccessException("Ütköző időpontot csak a Tulajdonos vagy a Menedzser erőszakolhat ki.");

            var variantIds = dto.Items.Select(i => i.ServiceVariantId).ToList();
            var (_, price) = await _bookingEngine.CalculateAppointmentDetailsAsync(dto.CustomerId, variantIds);

            int totalDuration = dto.Items.Sum(i => i.DurationMinutes);
            DateTime endDateTime = dto.StartDateTime.AddMinutes(totalDuration);

            bool isAvailable = await _bookingEngine.IsTimeSlotAvailableAsync(companyId, appointment.EmployeeId, dto.StartDateTime, endDateTime, dto.Force, appointment.Id);
            if (!isAvailable) throw new InvalidOperationException("A kiválasztott időpont ütközik egy másikkal.");

            appointment.CustomerId = dto.CustomerId;
            appointment.StartDateTime = dto.StartDateTime;
            appointment.EndDateTime = endDateTime;
            appointment.TotalPrice = price;
            appointment.Status = dto.Status;
            appointment.AdminNotes = dto.Notes;

            var oldItems = await _context.AppointmentItems.Where(i => i.AppointmentId == appointment.Id).ToListAsync();
            _context.AppointmentItems.RemoveRange(oldItems);
            await _context.SaveChangesAsync();

            foreach (var itemDto in dto.Items)
            {
                var variant = await _context.ServiceVariants.FindAsync(itemDto.ServiceVariantId);
                if (variant == null) throw new ArgumentException($"Hiba: A(z) {itemDto.ServiceVariantId} variáns nem található!");

                _context.AppointmentItems.Add(new AppointmentItem
                {
                    AppointmentId = appointment.Id,
                    ServiceVariantId = itemDto.ServiceVariantId,
                    Price = variant.Price,
                    CalculatedDurationMinutes = itemDto.DurationMinutes
                });
            }

            await _context.SaveChangesAsync();
            return appointment;
        }

        public async Task<bool> DeleteAppointmentAsync(int appointmentId, string username)
        {
            int companyId = _tenantContext.CurrentCompany?.Id ?? throw new Exception("Nincs kiválasztva cég.");

            var appointment = await _context.Appointments
                .FirstOrDefaultAsync(a => a.Id == appointmentId && a.CompanyId == companyId);

            if (appointment == null) return false;

            var currentUser = await _context.Users.SingleAsync(u => u.Username == username);
            var currentEmployee = await _context.CompanyEmployees
                .SingleOrDefaultAsync(e => e.UserId == currentUser.Id && e.CompanyId == companyId);

            if (currentEmployee == null) throw new UnauthorizedAccessException("Nincs jogosultságod.");

            if (currentEmployee.Role == EmployeeRole.Worker && appointment.EmployeeId != currentEmployee.Id)
                throw new UnauthorizedAccessException("Csak a saját időpontjaidat törölheted.");

            _context.Appointments.Remove(appointment);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}