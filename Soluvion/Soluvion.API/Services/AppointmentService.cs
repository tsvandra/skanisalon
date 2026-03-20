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

            // KÖTELEZŐ SaaS Szűrés (CompanyId) és időintervallum
            var query = _context.Appointments
                .Include(a => a.Items) // Fontos: Be kell vonni a tételeket, hogy a UI lássa a szolgáltatásokat!
                .Where(a => a.CompanyId == companyId && a.StartDateTime >= start && a.StartDateTime <= end);

            // Jogosultság alapú szűrés (RBAC)
            if (currentEmployee.Role == EmployeeRole.Worker)
            {
                query = query.Where(a => a.EmployeeId == currentEmployee.Id);
            }
            else if (employeeId.HasValue)
            {
                query = query.Where(a => a.EmployeeId == employeeId.Value);
            }

            // Adatbázis szintű (SQL) leképezés DTO-ba (Sokkal gyorsabb és hibamentes)
            var appointments = await query.Select(a => new AppointmentResponseDto
            {
                Id = a.Id,
                CustomerId = a.CustomerId,
                EmployeeId = a.EmployeeId,
                StartDateTime = a.StartDateTime,
                EndDateTime = a.EndDateTime,
                TotalPrice = a.TotalPrice,
                Status = a.Status.ToString(), // Biztosítjuk, hogy számként menjen ki, ahogy a UI várja (vagy stringként, ha azt használod)
                Notes = (a.CustomerNotes != null && a.CustomerNotes != "") ? a.CustomerNotes : a.AdminNotes,

                // Tételek átadása a UI-nak
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

            if (company!.SubscriptionPlan == SubscriptionPlan.Free)
            {
                throw new InvalidOperationException("Az okos időpontfoglaló modul használatához legalább Basic előfizetés szükséges.");
            }

            var currentUser = await _context.Users.SingleAsync(u => u.Username == username);
            var currentEmployee = await _context.CompanyEmployees
                .SingleOrDefaultAsync(e => e.UserId == currentUser.Id && e.CompanyId == companyId);

            if (currentEmployee == null)
                throw new UnauthorizedAccessException("Nem vagy hozzárendelve ehhez a céghez.");

            // 1. BIZTONSÁGI JAVÍTÁS AZ 500-AS HIBÁRA (Hardkódolt EmployeeId ellen)
            var targetEmployeeExists = await _context.CompanyEmployees.AnyAsync(e => e.Id == dto.EmployeeId && e.CompanyId == companyId);
            if (!targetEmployeeExists)
            {
                // Ha a Vue űrlap érvénytelen dolgozót küld (pl. fix 1-et), akkor ahhoz a dolgozóhoz mentjük, aki épp be van jelentkezve!
                dto.EmployeeId = currentEmployee.Id;
            }

            if (dto.Force && currentEmployee.Role != EmployeeRole.Owner && currentEmployee.Role != EmployeeRole.Manager)
            {
                throw new UnauthorizedAccessException("Ütköző időpontot csak a Tulajdonos vagy a Menedzser erőszakolhat ki (Force).");
            }

            // 2. Árazás és időtartam
            var variantIds = dto.Items.Select(i => i.ServiceVariantId).ToList();
            var (_, price) = await _bookingEngine.CalculateAppointmentDetailsAsync(dto.CustomerId, variantIds);

            int totalDuration = dto.Items.Sum(i => i.DurationMinutes);
            DateTime endDateTime = dto.StartDateTime.AddMinutes(totalDuration);

            // 3. Ütközésvizsgálat
            bool isAvailable = await _bookingEngine.IsTimeSlotAvailableAsync(companyId, dto.EmployeeId, dto.StartDateTime, endDateTime, dto.Force);
            if (!isAvailable) throw new InvalidOperationException("A kiválasztott időpont ütközik egy másikkal.");

            // 4. Foglalás létrehozása
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
            await _context.SaveChangesAsync(); // Ha itt volt korábban az 500-as hiba, az EmployeeId felülírásával most eltűnik!

            return appointment;
        }

        public async Task<Appointment> UpdateAppointmentAsync(int appointmentId, UpdateAppointmentDto dto, string username)
        {
            int companyId = _tenantContext.CurrentCompany?.Id ?? throw new Exception("Nincs kiválasztva cég.");

            var appointment = await _context.Appointments.Include(a => a.Items)
                                            .FirstOrDefaultAsync(a => a.Id == appointmentId && a.CompanyId == companyId);

            if (appointment == null) throw new KeyNotFoundException("Időpont nem található vagy nincs jogosultságod.");

            var currentUser = await _context.Users.SingleAsync(u => u.Username == username);
            var currentEmployee = await _context.CompanyEmployees
                .SingleOrDefaultAsync(e => e.UserId == currentUser.Id && e.CompanyId == companyId);

            if (currentEmployee == null) throw new InvalidOperationException("Nincs jogosultságod.");

            if (dto.Force && currentEmployee.Role != EmployeeRole.Owner && currentEmployee.Role != EmployeeRole.Manager)
                throw new InvalidOperationException("Ütköző időpontot csak a Tulajdonos vagy a Menedzser erőszakolhat ki.");

            // Árazás és időtartam
            var variantIds = dto.Items.Select(i => i.ServiceVariantId).ToList();
            var (_, price) = await _bookingEngine.CalculateAppointmentDetailsAsync(dto.CustomerId, variantIds);

            int totalDuration = dto.Items.Sum(i => i.DurationMinutes);
            DateTime endDateTime = dto.StartDateTime.AddMinutes(totalDuration);

            bool isAvailable = await _bookingEngine.IsTimeSlotAvailableAsync(companyId, appointment.EmployeeId, dto.StartDateTime, endDateTime, dto.Force, appointment.Id);
            if (!isAvailable) throw new InvalidOperationException("A kiválasztott időpont ütközik egy másikkal.");

            // Foglalás alap adatainak frissítése
            appointment.CustomerId = dto.CustomerId;
            appointment.StartDateTime = dto.StartDateTime;
            appointment.EndDateTime = endDateTime;
            appointment.TotalPrice = price;
            appointment.Status = dto.Status;
            appointment.AdminNotes = dto.Notes;

            // BIZTONSÁGI JAVÍTÁS A CONCURRENCY HIBÁRA: 
            // 1. Lépés: Explicit töröljük a régi elemeket és azonnal rámentünk, hogy az EF Core megnyugodjon
            var oldItems = await _context.AppointmentItems.Where(i => i.AppointmentId == appointment.Id).ToListAsync();
            _context.AppointmentItems.RemoveRange(oldItems);
            await _context.SaveChangesAsync();

            // 2. Lépés: Szépen, tisztán felvesszük az új tételeket
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

            await _context.SaveChangesAsync(); // Végső mentés
            return appointment;
        }

        public async Task<bool> DeleteAppointmentAsync(int appointmentId, string username)
        {
            int companyId = _tenantContext.CurrentCompany?.Id ?? throw new Exception("Nincs kiválasztva cég.");

            // Biztonság: Csak a céghez tartozó foglalás törölhető!
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