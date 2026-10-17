using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
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
    public class PublicBookingController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ISmartBookingEngine _bookingEngine;
        private readonly ITenantContext _tenantContext;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        public PublicBookingController(AppDbContext context, ISmartBookingEngine bookingEngine, ITenantContext tenantContext)
        {
            _context = context;
            _bookingEngine = bookingEngine;
            _tenantContext = tenantContext;
        }

        [HttpGet("schedule-config")]
        [Authorize]
        public async Task<IActionResult> GetScheduleConfig()
        {
            var company = _tenantContext.CurrentCompany;
            if (company == null)
            {
                return NotFound(new { errorCode = "COMPANY_NOT_FOUND" });
            }

            var dbCompany = await _context.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.Id == company.Id);
            var config = ParseScheduleConfig(dbCompany?.SettingsJson ?? company.SettingsJson);
            return Ok(config);
        }

        [HttpPut("schedule-config")]
        [Authorize]
        public async Task<IActionResult> UpdateScheduleConfig([FromBody] BookingScheduleConfigDto dto)
        {
            var company = _tenantContext.CurrentCompany;
            if (company == null)
            {
                return NotFound(new { errorCode = "COMPANY_NOT_FOUND" });
            }

            var dbCompany = await _context.Companies.FindAsync(company.Id);
            if (dbCompany == null)
            {
                return NotFound(new { errorCode = "COMPANY_NOT_FOUND" });
            }

            var normalized = NormalizeScheduleConfig(dto);
            dbCompany.SettingsJson = WriteScheduleConfigToSettings(dbCompany.SettingsJson, normalized);
            company.SettingsJson = dbCompany.SettingsJson;

            await _context.SaveChangesAsync();
            return Ok(normalized);
        }

        [HttpPut("day-override/{date}")]
        [Authorize]
        public async Task<IActionResult> UpdateDayOverride(string date, [FromBody] UpdateDayOverrideDto dto)
        {
            var company = _tenantContext.CurrentCompany;
            if (company == null)
            {
                return NotFound(new { errorCode = "COMPANY_NOT_FOUND" });
            }

            if (!DateOnly.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
            {
                return BadRequest(new { errorCode = "INVALID_DATE" });
            }

            var dbCompany = await _context.Companies.FindAsync(company.Id);
            if (dbCompany == null)
            {
                return NotFound(new { errorCode = "COMPANY_NOT_FOUND" });
            }

            var dateKey = parsedDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            var config = ParseScheduleConfig(dbCompany.SettingsJson);

            if (dto.ClearOverride)
            {
                config.DateOverrides.Remove(dateKey);
            }
            else
            {
                var normalizedIntervals = NormalizeDayIntervals(dto.Intervals);
                config.DateOverrides[dateKey] = new BookingDayScheduleDto
                {
                    IsOpen = dto.IsOpen && normalizedIntervals.Count > 0,
                    Intervals = normalizedIntervals
                };
            }

            dbCompany.SettingsJson = WriteScheduleConfigToSettings(dbCompany.SettingsJson, config);
            company.SettingsJson = dbCompany.SettingsJson;

            await _context.SaveChangesAsync();
            return Ok(config);
        }

        [HttpGet("availability")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAvailability(
            [FromQuery] string start,
            [FromQuery] string end,
            [FromQuery] int durationMinutes = 30,
            [FromQuery] int? employeeId = null)
        {
            var company = _tenantContext.CurrentCompany;
            if (company == null)
            {
                return NotFound(new { errorCode = "COMPANY_NOT_FOUND" });
            }

            if (!DateOnly.TryParseExact(start, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var startDate) ||
                !DateOnly.TryParseExact(end, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var endDate))
            {
                return BadRequest(new { errorCode = "INVALID_DATE_RANGE" });
            }

            if (endDate < startDate)
            {
                (startDate, endDate) = (endDate, startDate);
            }

            // Biztonsági korlát: legfeljebb 62 nap kérdezhető le egyszerre
            if (endDate.DayNumber - startDate.DayNumber > 62)
            {
                endDate = startDate.AddDays(62);
            }

            int reqDuration = Math.Max(15, durationMinutes);
            var dbCompany = await _context.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.Id == company.Id);
            var config = ParseScheduleConfig(dbCompany?.SettingsJson ?? company.SettingsJson);
            var bookableEmployees = await GetBookableEmployeesAsync(company.Id);

            int? targetEmployeeId = employeeId.HasValue && employeeId.Value > 0 ? employeeId.Value : null;
            if (!targetEmployeeId.HasValue)
            {
                if (bookableEmployees.Count > 0)
                {
                    targetEmployeeId = bookableEmployees[0].Id;
                }
                else
                {
                    var defaultEmp = await _context.CompanyEmployees
                        .AsNoTracking()
                        .Where(e => e.CompanyId == company.Id && e.IsActive)
                        .OrderBy(e => e.Id)
                        .FirstOrDefaultAsync();
                    targetEmployeeId = defaultEmp?.Id;
                }
            }

            var queryStartUtc = DateTime.SpecifyKind(startDate.ToDateTime(TimeOnly.MinValue).AddDays(-1), DateTimeKind.Utc);
            var queryEndUtc = DateTime.SpecifyKind(endDate.ToDateTime(TimeOnly.MaxValue).AddDays(1), DateTimeKind.Utc);

            var appointmentsQuery = _context.Appointments
                .AsNoTracking()
                .Where(a => a.CompanyId == company.Id &&
                            a.Status != AppointmentStatus.Cancelled &&
                            a.StartDateTime < queryEndUtc &&
                            a.EndDateTime > queryStartUtc);

            if (targetEmployeeId.HasValue)
            {
                appointmentsQuery = appointmentsQuery.Where(a => a.EmployeeId == targetEmployeeId.Value);
            }

            var rawAppointments = await appointmentsQuery
                .Select(a => new { a.StartDateTime, a.EndDateTime })
                .ToListAsync();

            var tz = GetSalonTimeZone();
            var localNow = ToSalonLocal(DateTime.UtcNow, tz);
            int leadMinutes = Math.Clamp(config.MinLeadTimeMinutes, 0, 10080);
            var earliestBookableLocal = localNow.AddMinutes(leadMinutes);
            var earliestDateOnly = DateOnly.FromDateTime(earliestBookableLocal);
            int earliestMinutesOnEarliestDay = earliestBookableLocal.Hour * 60 + earliestBookableLocal.Minute;

            var localAppointments = rawAppointments
                .Select(a => new
                {
                    Start = ToSalonLocal(a.StartDateTime, tz),
                    End = ToSalonLocal(a.EndDateTime, tz)
                })
                .Where(a => a.End > a.Start)
                .ToList();

            var resultDays = new List<DayAvailabilityDto>();

            for (var d = startDate; d <= endDate; d = d.AddDays(1))
            {
                string dateKey = d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                var (isOpen, hasCustomOverride, openRanges) = ResolveDayOpenRanges(d, dateKey, config);

                // Foglalt idősávok az adott napra [0..1440] közé vágva és összeolvasztva
                var dayStartDt = d.ToDateTime(TimeOnly.MinValue);
                var dayEndDt = dayStartDt.AddDays(1);

                var rawBusyRanges = new List<(int Start, int End)>();
                foreach (var app in localAppointments)
                {
                    if (app.End <= dayStartDt || app.Start >= dayEndDt) continue;

                    var clippedStart = app.Start < dayStartDt ? dayStartDt : app.Start;
                    var clippedEnd = app.End > dayEndDt ? dayEndDt : app.End;

                    int sMin = (int)Math.Floor((clippedStart - dayStartDt).TotalMinutes);
                    int eMin = (int)Math.Ceiling((clippedEnd - dayStartDt).TotalMinutes);
                    sMin = Math.Clamp(sMin, 0, 1440);
                    eMin = Math.Clamp(eMin, 0, 1440);
                    if (eMin > sMin)
                    {
                        rawBusyRanges.Add((sMin, eMin));
                    }
                }

                var mergedBusy = MergeRanges(rawBusyRanges);

                // Szabad idősávok = Nyitott sávok mínusz Foglalt sávok (és a minimum előfoglalási időn belüli / múltbeli idő)
                List<(int Start, int End)> freeRanges;
                if (!isOpen || openRanges.Count == 0 || d < earliestDateOnly)
                {
                    freeRanges = new List<(int Start, int End)>();
                }
                else
                {
                    var obstacles = new List<(int Start, int End)>(mergedBusy);
                    if (d == earliestDateOnly && earliestMinutesOnEarliestDay > 0)
                    {
                        obstacles.Add((0, Math.Min(1440, earliestMinutesOnEarliestDay)));
                    }
                    obstacles = MergeRanges(obstacles);
                    freeRanges = SubtractRanges(openRanges, obstacles);
                }

                // Van-e legalább egy 5 perces rácspontra illeszkedő szabad hely a kért időtartamnak?
                bool hasEnoughFreeTime = false;
                foreach (var free in freeRanges)
                {
                    int firstSlot = ((free.Start + 4) / 5) * 5;
                    if (firstSlot + reqDuration <= free.End)
                    {
                        hasEnoughFreeTime = true;
                        break;
                    }
                }

                // Terheltség (LoadPercentage) a nyitott idősávhoz képest
                int totalOpenMinutes = openRanges.Sum(r => r.End - r.Start);
                int coveredMinutes = 0;
                if (totalOpenMinutes > 0 && mergedBusy.Count > 0)
                {
                    var unbookedOpen = SubtractRanges(openRanges, mergedBusy);
                    int unbookedMinutes = unbookedOpen.Sum(r => r.End - r.Start);
                    coveredMinutes = Math.Max(0, totalOpenMinutes - unbookedMinutes);
                }
                int loadPct = totalOpenMinutes > 0
                    ? Math.Clamp((int)Math.Round((double)coveredMinutes * 100 / totalOpenMinutes), 0, 100)
                    : 0;

                resultDays.Add(new DayAvailabilityDto
                {
                    Date = dateKey,
                    IsOpen = isOpen && openRanges.Count > 0,
                    HasEnoughFreeTime = hasEnoughFreeTime,
                    HasCustomOverride = hasCustomOverride,
                    LoadPercentage = loadPct,
                    OpenIntervals = openRanges.Select(ToIntervalDto).ToList(),
                    BusyIntervals = mergedBusy.Select(ToIntervalDto).ToList(),
                    FreeIntervals = freeRanges.Select(ToIntervalDto).ToList()
                });
            }

            return Ok(new PublicAvailabilityResponseDto
            {
                Mode = config.Mode,
                MinLeadTimeMinutes = config.MinLeadTimeMinutes,
                Employees = bookableEmployees,
                Days = resultDays
            });
        }

        private async Task<List<PublicEmployeeDto>> GetBookableEmployeesAsync(int companyId)
        {
            var rawEmployees = await _context.CompanyEmployees
                .AsNoTracking()
                .Include(e => e.User)
                .Where(e => e.CompanyId == companyId && e.IsActive)
                .OrderBy(e => e.Id)
                .ToListAsync();

            return rawEmployees
                .Where(e => e.User != null
                    && !string.Equals(e.User.Role, "SuperAdmin", StringComparison.OrdinalIgnoreCase))
                .Select(e => new PublicEmployeeDto
                {
                    Id = e.Id,
                    Name = e.User!.Username
                })
                .ToList();
        }

        [HttpPost]
        [AllowAnonymous]
        [EnableRateLimiting("PublicBooking")]
        public async Task<IActionResult> CreateGuestBooking([FromBody] GuestBookingDto dto)
        {
            // Honeypot spam-védelem: ha a rejtett mező ki van töltve, elutasítjuk
            if (!string.IsNullOrWhiteSpace(dto.Website))
            {
                return BadRequest(new { errorCode = "SPAM_DETECTED" });
            }

            var company = _tenantContext.CurrentCompany;
            if (company == null)
            {
                return NotFound(new { errorCode = "COMPANY_NOT_FOUND" });
            }

            if (!company.IsOnlineBookingEnabled)
            {
                return StatusCode(403, new { errorCode = "ONLINE_BOOKING_DISABLED" });
            }

            int companyId = company.Id;

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Biztonságos Dolgozó (Employee) keresés (kizárva a teszt/SuperAdmin fiókokat)
                var bookableEmployees = await GetBookableEmployeesAsync(companyId);
                var employeeExists = bookableEmployees.Any(e => e.Id == dto.EmployeeId)
                    || await _context.CompanyEmployees.AnyAsync(e => e.CompanyId == companyId && e.Id == dto.EmployeeId && e.IsActive);

                if (!employeeExists || dto.EmployeeId <= 0)
                {
                    if (bookableEmployees.Count > 0)
                    {
                        dto.EmployeeId = bookableEmployees[0].Id;
                    }
                    else
                    {
                        var defaultEmployee = await _context.CompanyEmployees.FirstOrDefaultAsync(e => e.CompanyId == companyId && e.IsActive);
                        if (defaultEmployee == null)
                        {
                            return BadRequest(new { errorCode = "NO_ACTIVE_EMPLOYEE" });
                        }

                        dto.EmployeeId = defaultEmployee.Id;
                    }
                }

                // 2. Biztonságos JSONB Customer keresés
                var allCustomers = await _context.CompanyCustomers.Where(c => c.CompanyId == companyId).ToListAsync();
                var customer = allCustomers.FirstOrDefault(c => c.Attributes != null &&
                                                                c.Attributes.ContainsKey("Email") &&
                                                                c.Attributes["Email"] == dto.Email);

                if (customer == null)
                {
                    customer = new CompanyCustomer
                    {
                        CompanyId = companyId,
                        Attributes = dto.Attributes ?? new Dictionary<string, string>()
                    };
                    customer.Attributes["Email"] = dto.Email;
                    customer.Attributes["Phone"] = dto.Phone;
                    customer.Attributes["FullName"] = dto.FullName;
                    if (!string.IsNullOrWhiteSpace(dto.PreferredChannel))
                        customer.Attributes["PreferredChannel"] = dto.PreferredChannel.Trim();
                    if (!string.IsNullOrWhiteSpace(dto.MessengerHandle))
                        customer.Attributes["MessengerHandle"] = dto.MessengerHandle.Trim();
                    if (!string.IsNullOrWhiteSpace(dto.ClientLanguage))
                        customer.Attributes["PreferredLanguage"] = dto.ClientLanguage.Trim();

                    _context.CompanyCustomers.Add(customer);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    customer.Attributes ??= new Dictionary<string, string>();
                    if (dto.Attributes != null)
                    {
                        foreach (var attr in dto.Attributes)
                        {
                            customer.Attributes[attr.Key] = attr.Value;
                        }
                    }
                    if (!string.IsNullOrWhiteSpace(dto.Phone))
                        customer.Attributes["Phone"] = dto.Phone.Trim();
                    if (!string.IsNullOrWhiteSpace(dto.FullName))
                        customer.Attributes["FullName"] = dto.FullName.Trim();
                    if (!string.IsNullOrWhiteSpace(dto.PreferredChannel))
                        customer.Attributes["PreferredChannel"] = dto.PreferredChannel.Trim();
                    if (!string.IsNullOrWhiteSpace(dto.MessengerHandle))
                        customer.Attributes["MessengerHandle"] = dto.MessengerHandle.Trim();
                    if (!string.IsNullOrWhiteSpace(dto.ClientLanguage))
                        customer.Attributes["PreferredLanguage"] = dto.ClientLanguage.Trim();

                    _context.CompanyCustomers.Update(customer);
                    await _context.SaveChangesAsync();
                }

                // 3. Smart Booking Engine hívása + szigorú vendég-ütközésvizsgálat
                var (duration, price) = await _bookingEngine.CalculateAppointmentDetailsAsync(customer.Id, dto.ServiceVariantIds);
                int effectiveDuration = Math.Max(15, duration);
                DateTime endDateTime = dto.StartDateTime.AddMinutes(effectiveDuration);

                // A vendég sosem foglalhat már foglalt idősávra (akkor sem, ha az admin számára engedélyezett az átfedés)
                bool hasOverlap = await _context.Appointments.AnyAsync(a =>
                    a.CompanyId == companyId &&
                    a.EmployeeId == dto.EmployeeId &&
                    a.Status != AppointmentStatus.Cancelled &&
                    a.StartDateTime < endDateTime &&
                    a.EndDateTime > dto.StartDateTime);

                if (hasOverlap)
                {
                    return Conflict(new { errorCode = "BOOKING_SLOT_TAKEN" });
                }

                // Nyitvatartási idősáv és minimum előfoglalási idő (MinLeadTimeMinutes, alapért. 2 óra) ellenőrzése
                var tz = GetSalonTimeZone();
                var localNow = ToSalonLocal(DateTime.UtcNow, tz);
                var localStart = ToSalonLocal(dto.StartDateTime, tz);
                var localEnd = localStart.AddMinutes(effectiveDuration);
                var bookingDate = DateOnly.FromDateTime(localStart);
                string dateKey = bookingDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

                var dbCompany = await _context.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.Id == companyId);
                var config = ParseScheduleConfig(dbCompany?.SettingsJson ?? company.SettingsJson);

                int leadMinutes = Math.Clamp(config.MinLeadTimeMinutes, 0, 10080);
                // 5 perces türelmi idő az űrlap kitöltése közbeni peremhelyzetekre
                int effectiveLeadMinutes = Math.Max(0, leadMinutes - 5);
                if (localStart < localNow.AddMinutes(effectiveLeadMinutes))
                {
                    return BadRequest(new { errorCode = leadMinutes > 0 ? "BOOKING_TOO_SOON" : "BOOKING_IN_PAST" });
                }

                var (isOpen, _, openRanges) = ResolveDayOpenRanges(bookingDate, dateKey, config);

                int reqStartMin = localStart.Hour * 60 + localStart.Minute;
                int reqEndMin = reqStartMin + effectiveDuration;
                bool fitsInOpenWindow = isOpen && openRanges.Any(r => reqStartMin >= r.Start && reqEndMin <= r.End);
                if (!fitsInOpenWindow)
                {
                    return Conflict(new { errorCode = "BOOKING_SLOT_TAKEN" });
                }

                // 4. Appointment létrehozása a pontos beérkezési időbélyeggel (CreatedAt)
                var appointment = new Appointment
                {
                    CompanyId = companyId,
                    CustomerId = customer.Id,
                    EmployeeId = dto.EmployeeId,
                    StartDateTime = dto.StartDateTime,
                    EndDateTime = endDateTime,
                    TotalPrice = price,
                    Status = AppointmentStatus.Pending,
                    Source = BookingSource.Web,
                    CreatedAt = DateTime.UtcNow,
                    CustomerNotes = dto.Notes
                };

                foreach (var variantId in dto.ServiceVariantIds)
                {
                    var variant = await _context.ServiceVariants.FindAsync(variantId);
                    if (variant != null)
                    {
                        appointment.Items.Add(new AppointmentItem
                        {
                            ServiceVariantId = variantId,
                            Price = variant.Price,
                            CalculatedDurationMinutes = variant.Duration
                        });
                    }
                }

                _context.Appointments.Add(appointment);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { success = true, appointmentId = appointment.Id, createdAt = appointment.CreatedAt });
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { errorCode = "SERVER_ERROR" });
            }
        }

        // =====================================================================
        // SEGÉDMETÓDUSOK: Munkarend konfiguráció & Idősáv matematika
        // =====================================================================

        private static BookingScheduleConfigDto CreateDefaultScheduleConfig()
        {
            var config = new BookingScheduleConfigDto
            {
                Mode = "weekly_template",
                MinLeadTimeMinutes = 120,
                WeeklyHours = new Dictionary<string, BookingDayScheduleDto>(),
                DateOverrides = new Dictionary<string, BookingDayScheduleDto>()
            };

            for (int day = 1; day <= 7; day++)
            {
                bool isWeekday = day <= 5;
                config.WeeklyHours[day.ToString(CultureInfo.InvariantCulture)] = new BookingDayScheduleDto
                {
                    IsOpen = isWeekday,
                    Intervals = new List<BookingTimeIntervalDto>
                    {
                        new() { Start = "08:00", End = isWeekday ? "17:00" : "14:00" }
                    }
                };
            }

            return config;
        }

        private static BookingScheduleConfigDto ParseScheduleConfig(string? settingsJson)
        {
            var defaultConfig = CreateDefaultScheduleConfig();
            if (string.IsNullOrWhiteSpace(settingsJson))
            {
                return defaultConfig;
            }

            try
            {
                var node = JsonNode.Parse(settingsJson);
                if (node == null) return defaultConfig;

                BookingScheduleConfigDto? parsed = null;
                if (node["bookingSchedule"] != null)
                {
                    parsed = node["bookingSchedule"].Deserialize<BookingScheduleConfigDto>(JsonOptions);
                }
                else if (node["mode"] != null || node["Mode"] != null)
                {
                    parsed = node.Deserialize<BookingScheduleConfigDto>(JsonOptions);
                }

                return parsed != null ? NormalizeScheduleConfig(parsed) : defaultConfig;
            }
            catch
            {
                return defaultConfig;
            }
        }

        private static BookingScheduleConfigDto NormalizeScheduleConfig(BookingScheduleConfigDto input)
        {
            var result = CreateDefaultScheduleConfig();
            result.Mode = input.Mode == "custom_days" ? "custom_days" : "weekly_template";
            result.MinLeadTimeMinutes = Math.Clamp(input.MinLeadTimeMinutes, 0, 10080);

            if (input.WeeklyHours != null)
            {
                for (int day = 1; day <= 7; day++)
                {
                    string key = day.ToString(CultureInfo.InvariantCulture);
                    if (input.WeeklyHours.TryGetValue(key, out var daySched) && daySched != null)
                    {
                        var normalizedIntervals = NormalizeDayIntervals(daySched.Intervals);
                        if (normalizedIntervals.Count == 0)
                        {
                            normalizedIntervals.Add(new BookingTimeIntervalDto { Start = "08:00", End = "17:00" });
                        }
                        result.WeeklyHours[key] = new BookingDayScheduleDto
                        {
                            IsOpen = daySched.IsOpen,
                            Intervals = normalizedIntervals
                        };
                    }
                }
            }

            result.DateOverrides = new Dictionary<string, BookingDayScheduleDto>();
            if (input.DateOverrides != null)
            {
                foreach (var kvp in input.DateOverrides)
                {
                    if (!DateOnly.TryParseExact(kvp.Key, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
                    {
                        continue;
                    }
                    string dateKey = d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                    var normalizedIntervals = NormalizeDayIntervals(kvp.Value?.Intervals);
                    result.DateOverrides[dateKey] = new BookingDayScheduleDto
                    {
                        IsOpen = (kvp.Value?.IsOpen ?? false) && normalizedIntervals.Count > 0,
                        Intervals = normalizedIntervals
                    };
                }
            }

            return result;
        }

        private static List<BookingTimeIntervalDto> NormalizeDayIntervals(List<BookingTimeIntervalDto>? raw)
        {
            var ranges = new List<(int Start, int End)>();
            if (raw != null)
            {
                foreach (var item in raw)
                {
                    if (TryParseMinutes(item?.Start, out int s) && TryParseMinutes(item?.End, out int e) && e > s)
                    {
                        ranges.Add((s, e));
                    }
                }
            }

            var merged = MergeRanges(ranges);
            return merged.Select(r => new BookingTimeIntervalDto
            {
                Start = FormatMinutes(r.Start),
                End = FormatMinutes(r.End)
            }).ToList();
        }

        private static string WriteScheduleConfigToSettings(string? existingSettingsJson, BookingScheduleConfigDto config)
        {
            JsonObject root;
            try
            {
                root = (!string.IsNullOrWhiteSpace(existingSettingsJson)
                    ? JsonNode.Parse(existingSettingsJson) as JsonObject
                    : null) ?? new JsonObject();
            }
            catch
            {
                root = new JsonObject();
            }

            root["bookingSchedule"] = JsonSerializer.SerializeToNode(config, JsonOptions);
            return root.ToJsonString();
        }

        private static (bool IsOpen, bool HasCustomOverride, List<(int Start, int End)> OpenRanges) ResolveDayOpenRanges(
            DateOnly date,
            string dateKey,
            BookingScheduleConfigDto config)
        {
            if (config.DateOverrides != null && config.DateOverrides.TryGetValue(dateKey, out var overrideSched) && overrideSched != null)
            {
                if (!overrideSched.IsOpen)
                {
                    return (false, true, new List<(int Start, int End)>());
                }
                var ranges = ParseRanges(overrideSched.Intervals);
                return (ranges.Count > 0, true, ranges);
            }

            if (config.Mode == "custom_days")
            {
                return (false, false, new List<(int Start, int End)>());
            }

            // Heti sablon ("1" = Hétfő .. "7" = Vasárnap)
            int isoDayOfWeek = ((int)date.DayOfWeek + 6) % 7 + 1;
            string dowKey = isoDayOfWeek.ToString(CultureInfo.InvariantCulture);

            if (config.WeeklyHours != null && config.WeeklyHours.TryGetValue(dowKey, out var weeklySched) && weeklySched != null && weeklySched.IsOpen)
            {
                var ranges = ParseRanges(weeklySched.Intervals);
                return (ranges.Count > 0, false, ranges);
            }

            return (false, false, new List<(int Start, int End)>());
        }

        private static List<(int Start, int End)> ParseRanges(List<BookingTimeIntervalDto>? intervals)
        {
            var list = new List<(int Start, int End)>();
            if (intervals == null) return list;

            foreach (var iv in intervals)
            {
                if (TryParseMinutes(iv.Start, out int s) && TryParseMinutes(iv.End, out int e) && e > s)
                {
                    list.Add((s, e));
                }
            }
            return MergeRanges(list);
        }

        private static bool TryParseMinutes(string? hhmm, out int totalMinutes)
        {
            totalMinutes = 0;
            if (string.IsNullOrWhiteSpace(hhmm)) return false;
            var parts = hhmm.Trim().Split(':');
            if (parts.Length < 2) return false;
            if (!int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int h) ||
                !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int m))
            {
                return false;
            }
            if (h == 24 && m == 0)
            {
                totalMinutes = 1440;
                return true;
            }
            if (h < 0 || h > 23 || m < 0 || m > 59) return false;
            totalMinutes = h * 60 + m;
            return true;
        }

        private static string FormatMinutes(int minutes)
        {
            int clamped = Math.Clamp(minutes, 0, 1440);
            int h = clamped / 60;
            int m = clamped % 60;
            return $"{h:D2}:{m:D2}";
        }

        private static TimeIntervalMinutesDto ToIntervalDto((int Start, int End) range)
        {
            return new TimeIntervalMinutesDto
            {
                Start = FormatMinutes(range.Start),
                End = FormatMinutes(range.End),
                StartMinutes = range.Start,
                EndMinutes = range.End
            };
        }

        private static List<(int Start, int End)> MergeRanges(List<(int Start, int End)> ranges)
        {
            if (ranges.Count == 0) return new List<(int Start, int End)>();
            var sorted = ranges.Where(r => r.End > r.Start).OrderBy(r => r.Start).ThenBy(r => r.End).ToList();
            if (sorted.Count == 0) return new List<(int Start, int End)>();

            var merged = new List<(int Start, int End)> { sorted[0] };
            for (int i = 1; i < sorted.Count; i++)
            {
                var last = merged[^1];
                var curr = sorted[i];
                if (curr.Start <= last.End)
                {
                    merged[^1] = (last.Start, Math.Max(last.End, curr.End));
                }
                else
                {
                    merged.Add(curr);
                }
            }
            return merged;
        }

        private static List<(int Start, int End)> SubtractRanges(
            List<(int Start, int End)> sourceRanges,
            List<(int Start, int End)> subtractRanges)
        {
            var current = new List<(int Start, int End)>(sourceRanges);
            foreach (var sub in subtractRanges)
            {
                var next = new List<(int Start, int End)>();
                foreach (var src in current)
                {
                    if (sub.End <= src.Start || sub.Start >= src.End)
                    {
                        next.Add(src);
                    }
                    else
                    {
                        if (sub.Start > src.Start)
                        {
                            next.Add((src.Start, sub.Start));
                        }
                        if (sub.End < src.End)
                        {
                            next.Add((sub.End, src.End));
                        }
                    }
                }
                current = next;
                if (current.Count == 0) break;
            }
            return current;
        }

        private static TimeZoneInfo GetSalonTimeZone()
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Europe/Bratislava");
            }
            catch
            {
                try
                {
                    return TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time");
                }
                catch
                {
                    return TimeZoneInfo.Local;
                }
            }
        }

        private static DateTime ToSalonLocal(DateTime dt, TimeZoneInfo tz)
        {
            if (dt.Kind == DateTimeKind.Utc)
            {
                return TimeZoneInfo.ConvertTimeFromUtc(dt, tz);
            }
            if (dt.Kind == DateTimeKind.Local)
            {
                return TimeZoneInfo.ConvertTime(dt, tz);
            }
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(dt, DateTimeKind.Utc), tz);
        }
    }
}