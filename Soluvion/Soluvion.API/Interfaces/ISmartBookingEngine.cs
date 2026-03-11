namespace Soluvion.API.Interfaces
{
    public interface ISmartBookingEngine
    {
        Task<(int TotalDuration, decimal TotalPrice)> CalculateAppointmentDetailsAsync(int customerId, List<int> variantIds);
        Task<bool> IsTimeSlotAvailableAsync(int companyId, int employeeId, DateTime start, DateTime end, bool force, int? excludeAppointmentId = null);
    }
}