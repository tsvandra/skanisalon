using Microsoft.EntityFrameworkCore;
using Soluvion.API.Data;
using Soluvion.API.DTOs;
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

            // 1. KÖTELEZŐ SaaS Szűrés (CompanyId) és időintervallum
            var query = _context.Appointments
                .Where(a => a.CompanyId == companyId && a.StartDateTime >= start && a.StartDateTime <= end);

            // 2. Jogosultság alapú szűrés (RBAC)
            if (currentEmployee.Role == EmployeeRole.Worker)
            {
                query = query.Where(a => a.EmployeeId == currentEmployee.Id);
            }
            else if (employeeId.HasValue)
            {
                query = query.Where(a => a.EmployeeId == employeeId.Value);
            }

            // 3. Adatbázis szintű (SQL) leképezés DTO-ba a ToListAsync előtt (Sokkal gyorsabb és hibamentes)
            var appointments = await query.Select(a => new AppointmentResponseDto
            {
                Id = a.Id,
                CustomerId = a.CustomerId,
                EmployeeId = a.EmployeeId,
                StartDateTime = a.StartDateTime,
                EndDateTime = a.EndDateTime,
                TotalPrice = a.TotalPrice,
                Status = a.Status.ToString(),
                // Biztonságos EF Core null check (string.IsNullOrEmpty helyett)
                Notes = (a.CustomerNotes != null && a.CustomerNotes != "") ? a.CustomerNotes : a.AdminNotes
            }).ToListAsync();

            return appointments;
        }

        public async Task<Appointment> CreateAppointmentAsync(CreateAppointmentDto dto, string username)
        {
            int companyId = _tenantContext.CurrentCompany?.Id ?? throw new Exception("Nincs kiválasztva cég.");
            var company = await _context.Companies.FindAsync(companyId);

            if (company!.SubscriptionPlan == SubscriptionPlan.Free)
            {
                throw new UnauthorizedAccessException("Az okos időpontfoglaló modul használatához legalább Basic előfizetés szükséges.");
            }

            var currentUser = await _context.Users.SingleAsync(u => u.Username == username);
            var currentEmployee = await _context.CompanyEmployees
                .SingleOrDefaultAsync(e => e.UserId == currentUser.Id && e.CompanyId == companyId);

            if (currentEmployee == null)
                throw new UnauthorizedAccessException("Nem vagy hozzárendelve ehhez a céghez.");

            if (dto.Force && currentEmployee.Role != EmployeeRole.Owner && currentEmployee.Role != EmployeeRole.Manager)
            {
                throw new UnauthorizedAccessException("Ütköző időpontot csak a Tulajdonos vagy a Menedzser erőszakolhat ki (Force).");
            }

            if (currentEmployee.Role == EmployeeRole.Worker && dto.EmployeeId != currentEmployee.Id)
            {
                throw new UnauthorizedAccessException("Alkalmazottként csak magadhoz rögzíthetsz időpontot.");
            }

            var (duration, price) = await _bookingEngine.CalculateAppointmentDetailsAsync(dto.CustomerId, dto.ServiceVariantIds);
            DateTime endDateTime = dto.StartDateTime.AddMinutes(duration);

            bool isAvailable = await _bookingEngine.IsTimeSlotAvailableAsync(companyId, dto.EmployeeId, dto.StartDateTime, endDateTime, dto.Force);
            if (!isAvailable)
            {
                throw new InvalidOperationException("A kiválasztott időpont ütközik egy másikkal.");
            }

            // 6. Foglalás létrehozása (Új modellek)
            var appointment = new Appointment
            {
                CompanyId = companyId,
                CustomerId = dto.CustomerId,
                EmployeeId = dto.EmployeeId,
                StartDateTime = dto.StartDateTime,
                EndDateTime = endDateTime,
                TotalPrice = price,
                Status = AppointmentStatus.Confirmed,
                Source = BookingSource.System, // ÚJ: Rendszerből lett felvéve
                AdminNotes = dto.Notes // ÚJ: Belső, dolgozói megjegyzés
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

            return appointment;
        }

        public async Task<Appointment> UpdateAppointmentAsync(int appointmentId, UpdateAppointmentDto dto, string username)
        {
            int companyId = _tenantContext.CurrentCompany?.Id ?? throw new Exception("Nincs kiválasztva cég.");

            var appointment = await _context.Appointments.Include(a => a.Items)
                                            .FirstOrDefaultAsync(a => a.Id == appointmentId);
            if (appointment == null) throw new KeyNotFoundException("Időpont nem található.");

            var currentUser = await _context.Users.SingleAsync(u => u.Username == username);
            var currentEmployee = await _context.CompanyEmployees
                .SingleOrDefaultAsync(e => e.UserId == currentUser.Id && e.CompanyId == companyId);

            if (currentEmployee == null) throw new UnauthorizedAccessException("Nincs jogosultságod.");

            if (currentEmployee.Role == EmployeeRole.Worker && appointment.EmployeeId != currentEmployee.Id)
                throw new UnauthorizedAccessException("Csak a saját időpontjaidat módosíthatod.");

            if (dto.Force && currentEmployee.Role != EmployeeRole.Owner && currentEmployee.Role != EmployeeRole.Manager)
                throw new UnauthorizedAccessException("Ütköző időpontot csak a Tulajdonos vagy a Menedzser erőszakolhat ki (Force).");

            var (duration, price) = await _bookingEngine.CalculateAppointmentDetailsAsync(appointment.CustomerId, dto.ServiceVariantIds);
            DateTime endDateTime = dto.StartDateTime.AddMinutes(duration);

            bool isAvailable = await _bookingEngine.IsTimeSlotAvailableAsync(companyId, appointment.EmployeeId, dto.StartDateTime, endDateTime, dto.Force, appointment.Id);

            if (!isAvailable) throw new InvalidOperationException("A kiválasztott időpont ütközik egy másikkal.");

            // Frissítés
            appointment.StartDateTime = dto.StartDateTime;
            appointment.EndDateTime = endDateTime;
            appointment.TotalPrice = price;
            appointment.Status = dto.Status;
            appointment.AdminNotes = dto.Notes; // Módosításkor a belső megjegyzést mentjük felül

            _context.AppointmentItems.RemoveRange(appointment.Items);

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

            await _context.SaveChangesAsync();
            return appointment;
        }

        public async Task<bool> DeleteAppointmentAsync(int appointmentId, string username)
        {
            int companyId = _tenantContext.CurrentCompany?.Id ?? throw new Exception("Nincs kiválasztva cég.");
            var appointment = await _context.Appointments.FindAsync(appointmentId);
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