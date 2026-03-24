using Microsoft.EntityFrameworkCore;
using Soluvion.API.Data;
using Soluvion.API.Interfaces;
using Soluvion.Domain.Models.Enums;

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
            var variants = await _context.ServiceVariants
                                         .Where(v => variantIds.Contains(v.Id))
                                         .ToListAsync();

            // Szigorúan csak az adatbázisból jövő variánsok árait és időtartamait összegezzük!
            // (A dinamikus felárakat a jövőben egy külön adatbázis táblából fogjuk kezelni, ha szükséges.)
            decimal totalPrice = variants.Sum(v => v.Price);
            int totalDuration = variants.Sum(v => v.Duration);

            return (totalDuration, totalPrice);
        }

        public async Task<bool> IsTimeSlotAvailableAsync(int companyId, int employeeId, DateTime start, DateTime end, bool force, int? excludeAppointmentId = null)
        {
            var company = await _context.Companies.FindAsync(companyId);

            // Ha a cég eleve engedélyezi az ütközést, vagy az admin kényszeríti (force), akkor mehet.
            if (company != null && company.AllowOverlappingAppointments) return true;
            if (force) return true;

            // Megnézzük, van-e már foglalás erre az idősávra (ami nincs lemondva)
            var query = _context.Appointments
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