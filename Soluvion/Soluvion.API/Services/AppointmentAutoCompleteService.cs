using Microsoft.EntityFrameworkCore;
using Soluvion.API.Data;
using Soluvion.Domain.Models.Enums;

namespace Soluvion.API.Services
{
    /// <summary>
    /// Háttérszolgáltatás: a már lejárt, megerősített (Confirmed) foglalásokat automatikusan befejezettre (Completed) állítja.
    /// Minden tenantra fut (nincs tenant kontextus, ezért a query filter nem szűkít).
    /// A készletlevonást NEM érinti: az a napi zárás (és a Company.StockTrackingStartDate) dolga.
    /// </summary>
    public class AppointmentAutoCompleteService : BackgroundService
    {
        private static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AppointmentAutoCompleteService> _logger;
        private readonly int _graceMinutes;

        public AppointmentAutoCompleteService(
            IServiceScopeFactory scopeFactory,
            ILogger<AppointmentAutoCompleteService> logger,
            IConfiguration configuration)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            // Türelmi idő a foglalás vége után (pl. hogy a "nem jelent meg" még jelölhető legyen). Alapértelmezett: 30 perc.
            _graceMinutes = Math.Max(0, configuration.GetValue("AppointmentAutoComplete:GraceMinutes", 30));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Rövid késleltetés, hogy az alkalmazás indulása ne ütközzön a migrációkkal
            try { await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken); }
            catch (OperationCanceledException) { return; }

            using var timer = new PeriodicTimer(Interval);
            do
            {
                try
                {
                    await CompleteFinishedAppointmentsAsync(stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Hiba a lejárt foglalások automatikus befejezésekor.");
                }
            }
            while (await WaitForNextTickAsync(timer, stoppingToken));
        }

        private static async Task<bool> WaitForNextTickAsync(PeriodicTimer timer, CancellationToken ct)
        {
            try { return await timer.WaitForNextTickAsync(ct); }
            catch (OperationCanceledException) { return false; }
        }

        private async Task CompleteFinishedAppointmentsAsync(CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var cutoff = DateTime.UtcNow.AddMinutes(-_graceMinutes);

            var updated = await context.Appointments
                .IgnoreQueryFilters()
                .Where(a => a.Status == AppointmentStatus.Confirmed && a.EndDateTime <= cutoff)
                .ExecuteUpdateAsync(s => s.SetProperty(a => a.Status, AppointmentStatus.Completed), ct);

            if (updated > 0)
            {
                _logger.LogInformation("{Count} lejárt foglalás automatikusan befejezettre állítva.", updated);
            }
        }
    }
}

