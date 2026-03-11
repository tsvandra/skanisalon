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

            var query = _context.Appointments
                .Where(a => a.StartDateTime >= start && a.StartDateTime <= end);

            // Ha a bejelentkezett felhasználó csak Worker, KIZÁRÓLAG a saját időpontjait láthatja!
            if (currentEmployee.Role == EmployeeRole.Worker)
            {
                query = query.Where(a => a.EmployeeId == currentEmployee.Id);
            }
            // Ha Admin/Manager, de szűrni akar egy adott dolgozóra:
            else if (employeeId.HasValue)
            {
                query = query.Where(a => a.EmployeeId == employeeId.Value);
            }

            var appointments = await query.ToListAsync();

            return appointments.Select(a => new AppointmentResponseDto
            {
                Id = a.Id,
                CustomerId = a.CustomerId,
                EmployeeId = a.EmployeeId,
                StartDateTime = a.StartDateTime,
                EndDateTime = a.EndDateTime,
                TotalPrice = a.TotalPrice,
                Status = a.Status.ToString(),
                Notes = a.Notes
            }).ToList();
        }

        public async Task<Appointment> CreateAppointmentAsync(CreateAppointmentDto dto, string username)
        {
            int companyId = _tenantContext.CurrentCompany?.Id ?? throw new Exception("Nincs kiválasztva cég.");
            var company = await _context.Companies.FindAsync(companyId);

            // 1. SaaS Subscription Plan Ellenőrzés
            if (company!.SubscriptionPlan == SubscriptionPlan.Free)
            {
                throw new UnauthorizedAccessException("Az okos időpontfoglaló modul használatához legalább Basic előfizetés szükséges.");
            }

            // 2. Bejelentkezett felhasználó Role ellenőrzése a cégben
            var currentUser = await _context.Users.SingleAsync(u => u.Username == username);
            var currentEmployee = await _context.CompanyEmployees
                .SingleOrDefaultAsync(e => e.UserId == currentUser.Id && e.CompanyId == companyId);

            if (currentEmployee == null)
                throw new UnauthorizedAccessException("Nem vagy hozzárendelve ehhez a céghez.");

            // 3. RBAC (Role-Based Access Control) ellenőrzés
            if (dto.Force && currentEmployee.Role != EmployeeRole.Owner && currentEmployee.Role != EmployeeRole.Manager)
            {
                throw new UnauthorizedAccessException("Ütköző időpontot csak a Tulajdonos vagy a Menedzser erőszakolhat ki (Force).");
            }

            if (currentEmployee.Role == EmployeeRole.Worker && dto.EmployeeId != currentEmployee.Id)
            {
                throw new UnauthorizedAccessException("Alkalmazottként csak magadhoz rögzíthetsz időpontot.");
            }

            // 4. Kalkuláció
            var (duration, price) = await _bookingEngine.CalculateAppointmentDetailsAsync(dto.CustomerId, dto.ServiceVariantIds);
            DateTime endDateTime = dto.StartDateTime.AddMinutes(duration);

            // 5. Elérhetőség vizsgálata
            bool isAvailable = await _bookingEngine.IsTimeSlotAvailableAsync(companyId, dto.EmployeeId, dto.StartDateTime, endDateTime, dto.Force);
            if (!isAvailable)
            {
                throw new InvalidOperationException("A kiválasztott időpont ütközik egy másikkal.");
            }

            // 6. Foglalás létrehozása
            var appointment = new Appointment
            {
                CompanyId = companyId,
                CustomerId = dto.CustomerId,
                EmployeeId = dto.EmployeeId,
                StartDateTime = dto.StartDateTime,
                EndDateTime = endDateTime,
                TotalPrice = price,
                Status = AppointmentStatus.Confirmed,
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

            // RBAC
            if (currentEmployee.Role == EmployeeRole.Worker && appointment.EmployeeId != currentEmployee.Id)
                throw new UnauthorizedAccessException("Csak a saját időpontjaidat módosíthatod.");

            if (dto.Force && currentEmployee.Role != EmployeeRole.Owner && currentEmployee.Role != EmployeeRole.Manager)
                throw new UnauthorizedAccessException("Ütköző időpontot csak a Tulajdonos vagy a Menedzser erőszakolhat ki (Force).");

            // Új kalkuláció
            var (duration, price) = await _bookingEngine.CalculateAppointmentDetailsAsync(appointment.CustomerId, dto.ServiceVariantIds);
            DateTime endDateTime = dto.StartDateTime.AddMinutes(duration);

            // Ütközés vizsgálata (Saját ID-t átadjuk kivételként!)
            bool isAvailable = await _bookingEngine.IsTimeSlotAvailableAsync(companyId, appointment.EmployeeId, dto.StartDateTime, endDateTime, dto.Force, appointment.Id);

            if (!isAvailable) throw new InvalidOperationException("A kiválasztott időpont ütközik egy másikkal.");

            // Frissítés
            appointment.StartDateTime = dto.StartDateTime;
            appointment.EndDateTime = endDateTime;
            appointment.TotalPrice = price;
            appointment.Status = dto.Status;
            appointment.Notes = dto.Notes;

            // Items frissítése (egyszerűség kedvéért töröljük a régieket és újra felvesszük)
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