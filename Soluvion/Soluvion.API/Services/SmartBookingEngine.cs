using Microsoft.EntityFrameworkCore;
using Soluvion.API.Data;
using Soluvion.API.Interfaces;
using Soluvion.Domain.Models.Enums;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Soluvion.API.Services
{
    public class SmartBookingEngine : ISmartBookingEngine
    {
        private readonly AppDbContext _context;

        public SmartBookingEngine(AppDbContext context)
        {
            _context = context;
        }

        public async Task<(int TotalDuration, decimal TotalPrice)> CalculateAppointmentDetailsAsync(int customerId, List<int> variantIds)
        {
            var customer = await _context.CompanyCustomers.FindAsync(customerId);
            var variants = await _context.ServiceVariants
                                         .Where(v => variantIds.Contains(v.Id))
                                         .ToListAsync();

            decimal totalPrice = variants.Sum(v => v.Price);
            int totalDuration = variants.Sum(v => v.Duration);

            // Dinamikus időszámítás a vendég JSONB attribútumai alapján
            // Példa: ha a vendég profiljában be van állítva a hajhossz

            // TODO - az attributumoknak az adatbazisbol kellene jönniük, nem hardcoded string-ként
            // Például: "HairLength": "Long" vagy "HairLength": "ExtraLong"
            if (customer != null && customer.Attributes.TryGetValue("HairLength", out string? hairLength))
            {
                if (hairLength.Equals("Long", StringComparison.OrdinalIgnoreCase))
                {
                    totalDuration += 30; // +30 perc hosszú haj esetén
                }
                else if (hairLength.Equals("ExtraLong", StringComparison.OrdinalIgnoreCase))
                {
                    totalDuration += 45; // +45 perc extra hosszú haj esetén
                }
            }

            return (totalDuration, totalPrice);
        }

        public async Task<bool> IsTimeSlotAvailableAsync(int companyId, int employeeId, DateTime start, DateTime end, bool force, int? excludeAppointmentId = null)
        {
            var company = await _context.Companies.FindAsync(companyId);

            // Ha a cég eleve engedélyezi az ütközést, vagy az admin kényszeríti (force), akkor mehet.
            if (company != null && company.AllowOverlappingAppointments) return true;
            if (force) return true;

            // Megnézzük, van-e már foglalás erre az idősávra (ami nincs lemondva)
            var query =  _context.Appointments
                .Where(a => a.EmployeeId == employeeId &&
                               a.Status != AppointmentStatus.Cancelled &&
                               a.StartDateTime < end &&
                               a.EndDateTime > start);
            
            if (excludeAppointmentId.HasValue)
            {
                query = query.Where(a => a.Id != excludeAppointmentId.Value);
            }

            bool isOverlapping = await query.AnyAsync();
            return !isOverlapping;
        }
    }
}