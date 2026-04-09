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

            int totalDuration = dto.Items.Sum(i => i.DurationMinutes);
            DateTime endDateTime = dto.StartDateTime.AddMinutes(totalDuration);

            bool isAvailable = await _bookingEngine.IsTimeSlotAvailableAsync(companyId, dto.EmployeeId, dto.StartDateTime, endDateTime, dto.Force);
            if (!isAvailable)
            {
                throw new InvalidOperationException("A kiválasztott időpont ütközik egy másikkal.");
            }

            // ÁR FELÜLBÍRÁLÁS: Admin felületről jövő konkrét (akár módosított) árak összegzése
            decimal totalPrice = dto.Items.Sum(i => i.Price);

            var appointment = new Appointment
            {
                CompanyId = companyId,
                CustomerId = dto.CustomerId,
                EmployeeId = dto.EmployeeId,
                StartDateTime = dto.StartDateTime,
                EndDateTime = endDateTime,
                TotalPrice = totalPrice,
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
                    // Ha a frontend küldött árat (akár módosítottat), azt mentjük. Ha nem, akkor az alapárat.
                    Price = itemDto.Price >= 0 ? itemDto.Price : variant.Price,
                    CalculatedDurationMinutes = itemDto.DurationMinutes
                });
            }

            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();

            // === A VARÁZSLAT ===
            // Szinkronizáljuk a vendég profilját a variánsok "ProfileModifiers" szabályai alapján
            await SyncCustomerAttributesFromVariantsAsync(dto.CustomerId, variantIds);

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

            if (currentEmployee == null)
                throw new UnauthorizedAccessException("Nincs jogosultságod.");

            if (dto.Force && currentEmployee.Role != EmployeeRole.Owner && currentEmployee.Role != EmployeeRole.Manager)
                throw new UnauthorizedAccessException("Ütköző időpontot csak a Tulajdonos vagy a Menedzser erőszakolhat ki.");

            var variantIds = dto.Items.Select(i => i.ServiceVariantId).ToList();

            int totalDuration = dto.Items.Sum(i => i.DurationMinutes);
            DateTime endDateTime = dto.StartDateTime.AddMinutes(totalDuration);

            bool isAvailable = await _bookingEngine.IsTimeSlotAvailableAsync(companyId, appointment.EmployeeId, dto.StartDateTime, endDateTime, dto.Force, appointment.Id);
            if (!isAvailable) throw new InvalidOperationException("A kiválasztott időpont ütközik egy másikkal.");

            // ÁR FELÜLBÍRÁLÁS: Admin felületről jövő konkrét (akár módosított) árak összegzése
            decimal totalPrice = dto.Items.Sum(i => i.Price);

            appointment.CustomerId = dto.CustomerId;
            appointment.StartDateTime = dto.StartDateTime;
            appointment.EndDateTime = endDateTime;
            appointment.TotalPrice = totalPrice;
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
                    Price = itemDto.Price >= 0 ? itemDto.Price : variant.Price,
                    CalculatedDurationMinutes = itemDto.DurationMinutes
                });
            }

            await _context.SaveChangesAsync();

            // === A VARÁZSLAT ===
            await SyncCustomerAttributesFromVariantsAsync(dto.CustomerId, variantIds);

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

        // === PRIVÁT METÓDUS: Az Automatikus Vendégprofil Frissítő Algoritmus ===
        private async Task SyncCustomerAttributesFromVariantsAsync(int customerId, List<int> variantIds)
        {
            var customer = await _context.CompanyCustomers.FindAsync(customerId);
            if (customer == null) return;

            var variants = await _context.ServiceVariants
                .Where(v => variantIds.Contains(v.Id))
                .ToListAsync();

            bool isCustomerChanged = false;

            // Biztosítjuk, hogy a JSONB szótár létezzen
            if (customer.Attributes == null)
            {
                customer.Attributes = new Dictionary<string, string>();
            }

            foreach (var variant in variants)
            {
                // Ha a variánsnak vannak profil-módosító szabályai
                if (variant.ProfileModifiers != null && variant.ProfileModifiers.Any())
                {
                    foreach (var modifier in variant.ProfileModifiers)
                    {
                        // Ha a kulcs még nem létezik, vagy más az értéke, felülírjuk!
                        if (!customer.Attributes.ContainsKey(modifier.Key) || customer.Attributes[modifier.Key] != modifier.Value)
                        {
                            customer.Attributes[modifier.Key] = modifier.Value;
                            isCustomerChanged = true;
                        }
                    }
                }
            }

            // Csak akkor hívunk adatbázis mentést (I/O műveletet), ha tényleg változott adat a vendégen
            if (isCustomerChanged)
            {
                _context.CompanyCustomers.Update(customer);
                await _context.SaveChangesAsync();
            }
        }
    }
}