using System.ClientModel;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using OpenAI.Chat;
using Soluvion.API.Data;
using Soluvion.API.DTOs.AppointmentDtos;
using Soluvion.API.Interfaces;
using Soluvion.Domain.Models;
using Soluvion.Domain.Models.Enums;

namespace Soluvion.API.Services
{
    public class AiChatBookingService : IAiChatBookingService
    {
        private readonly AppDbContext _context;
        private readonly ITenantContext _tenantContext;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<AiChatBookingService> _logger;

        public AiChatBookingService(
            AppDbContext context,
            ITenantContext tenantContext,
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory,
            ILogger<AiChatBookingService> logger)
        {
            _context = context;
            _tenantContext = tenantContext;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<AiChatBookingParseResultDto> ParseChatBookingAsync(
            string? messagesText,
            IEnumerable<IFormFile>? images,
            string? channelHint = null)
        {
            int companyId = _tenantContext.CurrentCompany?.Id ?? throw new InvalidOperationException("COMPANY_NOT_FOUND");

            var imageList = new List<(byte[] Bytes, string ContentType)>();
            if (images != null)
            {
                foreach (var file in images)
                {
                    if (file.Length <= 0) continue;
                    using var ms = new MemoryStream();
                    await file.CopyToAsync(ms);
                    var ct = string.IsNullOrWhiteSpace(file.ContentType) || !file.ContentType.StartsWith("image/")
                        ? "image/jpeg"
                        : file.ContentType;
                    imageList.Add((ms.ToArray(), ct));
                }
            }

            if (string.IsNullOrWhiteSpace(messagesText) && imageList.Count == 0)
            {
                throw new ArgumentException("EMPTY_CHAT_INPUT");
            }

            // 1. Szalon szolgáltatások és variánsok betöltése
            var services = await _context.Services
                .AsNoTracking()
                .Include(s => s.Variants)
                .Where(s => s.CompanyId == companyId)
                .ToListAsync();

            var catalogEntries = new List<(int VariantId, string Label, decimal Price, int Duration)>();
            foreach (var srv in services)
            {
                var srvNames = FormatDictValues(srv.Name);
                foreach (var v in srv.Variants.Where(x => x.Price > 0))
                {
                    var varNames = FormatDictValues(v.VariantName);
                    var fullLabel = string.IsNullOrWhiteSpace(varNames) ? srvNames : $"{srvNames} - {varNames}";
                    catalogEntries.Add((v.Id, fullLabel, v.Price, v.Duration));
                }
            }

            // 2. Meglévő vendégek betöltése
            var customers = await _context.CompanyCustomers
                .AsNoTracking()
                .Where(c => c.CompanyId == companyId)
                .ToListAsync();

            var customerEntries = customers
                .Select(c => new
                {
                    c.Id,
                    FullName = c.Attributes != null && c.Attributes.TryGetValue("FullName", out var fn) ? fn : "",
                    Phone = c.Attributes != null && c.Attributes.TryGetValue("Phone", out var ph) ? ph : "",
                    Email = c.Attributes != null && c.Attributes.TryGetValue("Email", out var em) ? em : ""
                })
                .Where(c => !string.IsNullOrWhiteSpace(c.FullName) || !string.IsNullOrWhiteSpace(c.Phone))
                .ToList();

            // 3. Naptári referencia a következő 21 napra (hogy a "holnap", "jövő kedden", "zajtra", "v piatok" pontos dátumra forduljon)
            var nowLocal = GetSalonLocalNow();
            var calendarRefBuilder = new StringBuilder();
            var huCulture = new CultureInfo("hu-HU");
            var skCulture = new CultureInfo("sk-SK");
            for (int i = 0; i <= 21; i++)
            {
                var d = nowLocal.Date.AddDays(i);
                var huDay = d.ToString("dddd", huCulture);
                var skDay = d.ToString("dddd", skCulture);
                var enDay = d.ToString("dddd", CultureInfo.InvariantCulture);
                var relLabel = i == 0 ? " (MA / DNES / TODAY)" : i == 1 ? " (HOLNAP / ZAJTRA / TOMORROW)" : i == 2 ? " (HOLNAPUTÁN / POZAJTRA)" : "";
                calendarRefBuilder.AppendLine($"- {d:yyyy-MM-dd}: {huDay} / {skDay} / {enDay}{relLabel}");
            }

            var catalogRefBuilder = new StringBuilder();
            foreach (var item in catalogEntries)
            {
                catalogRefBuilder.AppendLine($"- VariantId: {item.VariantId} | Name: {item.Label} | Duration: {item.Duration} min | Price: {item.Price} EUR");
            }

            var customerRefBuilder = new StringBuilder();
            foreach (var cust in customerEntries.Take(200))
            {
                customerRefBuilder.AppendLine($"- CustomerId: {cust.Id} | Name: {cust.FullName} | Phone: {cust.Phone}");
            }

            var systemPrompt = BuildSystemPrompt(
                nowLocal,
                calendarRefBuilder.ToString(),
                catalogRefBuilder.ToString(),
                customerRefBuilder.ToString(),
                channelHint);

            var geminiKey = _configuration["Gemini:ApiKey"] ?? _configuration["AI:GeminiApiKey"];
            var openAiKey = _configuration["OpenAI:ApiKey"];

            try
            {
                if (!string.IsNullOrWhiteSpace(geminiKey) && !geminiKey.Contains("IDE_JON"))
                {
                    var json = await CallGeminiAsync(imageList, messagesText, geminiKey, systemPrompt);
                    return ParseAiJson(json, catalogEntries, customerEntries, nowLocal, messagesText, channelHint);
                }

                if (!string.IsNullOrWhiteSpace(openAiKey) && !openAiKey.Contains("IDE_JON"))
                {
                    var json = await CallOpenAiAsync(imageList, messagesText, openAiKey, systemPrompt);
                    return ParseAiJson(json, catalogEntries, customerEntries, nowLocal, messagesText, channelHint);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "AI chat elemzés hiba, átváltás a helyi intelligens szabály-alapú elemzőre.");
            }

            // Okos helyi (offline / API-kulcs nélküli) elemző fallback
            return HeuristicParse(messagesText ?? "", catalogEntries, customerEntries.Select(c => (c.Id, c.FullName, c.Phone)).ToList(), nowLocal, channelHint);
        }

        public async Task<UpdatePendingFromChatResultDto> UpdatePendingAppointmentFromChatAsync(
            int appointmentId,
            string? messagesText,
            IEnumerable<IFormFile>? images = null,
            string? channelHint = null)
        {
            int companyId = _tenantContext.CurrentCompany?.Id ?? throw new InvalidOperationException("COMPANY_NOT_FOUND");

            var appointment = await _context.Appointments
                .Include(a => a.Items)
                .Include(a => a.Customer)
                .FirstOrDefaultAsync(a => a.Id == appointmentId && a.CompanyId == companyId);

            if (appointment == null)
            {
                throw new KeyNotFoundException("NOT_FOUND");
            }

            if (appointment.Status != AppointmentStatus.Pending)
            {
                throw new InvalidOperationException("ONLY_PENDING_CAN_BE_UPDATED_FROM_CHAT");
            }

            var parsed = await ParseChatBookingAsync(messagesText, images, channelHint);

            var tz = GetSalonTimeZone();
            var existingLocalStart = tz != null
                ? TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(appointment.StartDateTime, DateTimeKind.Utc), tz)
                : appointment.StartDateTime.AddHours(2);

            DateOnly targetDate = DateOnly.FromDateTime(existingLocalStart);
            TimeOnly targetTime = TimeOnly.FromDateTime(existingLocalStart);

            var changes = new List<string>();

            if (!string.IsNullOrWhiteSpace(parsed.Date) &&
                !parsed.MissingFields.Contains("date") &&
                DateOnly.TryParseExact(parsed.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var newDate))
            {
                if (newDate != targetDate)
                {
                    targetDate = newDate;
                    changes.Add($"Dátum: {targetDate:yyyy.MM.dd.}");
                }
            }

            if (!string.IsNullOrWhiteSpace(parsed.Time) &&
                !parsed.MissingFields.Contains("time") &&
                TimeOnly.TryParseExact(parsed.Time, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var newTime))
            {
                if (newTime != targetTime)
                {
                    targetTime = newTime;
                    changes.Add($"Időpont: {targetTime:HH:mm}");
                }
            }

            var mergedLocal = targetDate.ToDateTime(targetTime, DateTimeKind.Unspecified);
            var newStartUtc = tz != null
                ? TimeZoneInfo.ConvertTimeToUtc(mergedLocal, tz)
                : DateTime.SpecifyKind(mergedLocal.AddHours(-2), DateTimeKind.Utc);

            int totalDuration = appointment.Items.Sum(i => i.CalculatedDurationMinutes);
            if (totalDuration <= 0)
            {
                totalDuration = Math.Max(30, (int)(appointment.EndDateTime - appointment.StartDateTime).TotalMinutes);
            }

            if (parsed.ServiceVariantIds != null && parsed.ServiceVariantIds.Count > 0 && !parsed.MissingFields.Contains("services"))
            {
                var existingIds = appointment.Items.Select(i => i.ServiceVariantId).OrderBy(x => x).ToList();
                var newIds = parsed.ServiceVariantIds.Distinct().OrderBy(x => x).ToList();

                if (!existingIds.SequenceEqual(newIds))
                {
                    var matchedVariants = await _context.ServiceVariants
                        .Include(v => v.Service)
                        .Where(v => newIds.Contains(v.Id) && v.Service != null && v.Service.CompanyId == companyId)
                        .ToListAsync();

                    if (matchedVariants.Count > 0)
                    {
                        _context.AppointmentItems.RemoveRange(appointment.Items);
                        appointment.Items.Clear();

                        totalDuration = 0;
                        decimal newTotalPrice = 0;
                        foreach (var v in matchedVariants)
                        {
                            int dur = v.Duration > 0 ? v.Duration : 30;
                            totalDuration += dur;
                            newTotalPrice += v.Price;

                            appointment.Items.Add(new AppointmentItem
                            {
                                AppointmentId = appointment.Id,
                                ServiceVariantId = v.Id,
                                Price = v.Price,
                                CalculatedDurationMinutes = dur
                            });
                        }

                        appointment.TotalPrice = newTotalPrice;
                        changes.Add($"Szolgáltatások ({matchedVariants.Count} db, {newTotalPrice} EUR)");
                    }
                }
            }

            appointment.StartDateTime = newStartUtc;
            appointment.EndDateTime = newStartUtc.AddMinutes(Math.Max(15, totalDuration));

            var cleanChatNote = (parsed.Notes ?? messagesText ?? "").Trim();
            if (!string.IsNullOrWhiteSpace(cleanChatNote))
            {
                if (cleanChatNote.Length > 240) cleanChatNote = cleanChatNote.Substring(0, 240) + "...";
                var existingNote = (appointment.CustomerNotes ?? "").Trim();
                // Előző chat-frissítés cseréje vagy hozzáfűzése
                int prevIdx = existingNote.IndexOf("[💬 Chat:", StringComparison.Ordinal);
                if (prevIdx >= 0)
                {
                    existingNote = existingNote.Substring(0, prevIdx).Trim();
                }

                appointment.CustomerNotes = string.IsNullOrWhiteSpace(existingNote)
                    ? $"[💬 Chat: {cleanChatNote}]"
                    : $"{existingNote} | [💬 Chat: {cleanChatNote}]";
                changes.Add("Megjegyzés frissítve");
            }

            if (appointment.Customer != null && !string.IsNullOrWhiteSpace(parsed.Channel))
            {
                appointment.Customer.Attributes ??= new Dictionary<string, string>();
                appointment.Customer.Attributes["PreferredChannel"] = parsed.Channel;
                _context.CompanyCustomers.Update(appointment.Customer);
            }

            await _context.SaveChangesAsync();

            var responseDto = new AppointmentResponseDto
            {
                Id = appointment.Id,
                CustomerId = appointment.CustomerId,
                EmployeeId = appointment.EmployeeId,
                StartDateTime = appointment.StartDateTime,
                EndDateTime = appointment.EndDateTime,
                TotalPrice = appointment.TotalPrice,
                Status = appointment.Status.ToString(),
                MaterialUsageRecorded = appointment.MaterialUsageRecorded,
                ExtraMaterials = appointment.ExtraMaterials,
                CustomerName = appointment.Customer?.Attributes != null && appointment.Customer.Attributes.TryGetValue("FullName", out var fn) ? fn : "Ismeretlen",
                CustomerPhone = appointment.Customer?.Attributes != null && appointment.Customer.Attributes.TryGetValue("Phone", out var ph) ? ph : null,
                CustomerEmail = appointment.Customer?.Attributes != null && appointment.Customer.Attributes.TryGetValue("Email", out var em) ? em : null,
                PreferredChannel = appointment.Customer?.Attributes != null && appointment.Customer.Attributes.TryGetValue("PreferredChannel", out var pc) ? pc : null,
                MessengerHandle = appointment.Customer?.Attributes != null && appointment.Customer.Attributes.TryGetValue("MessengerHandle", out var mh) ? mh : null,
                PreferredLanguage = appointment.Customer?.Attributes != null && appointment.Customer.Attributes.TryGetValue("PreferredLanguage", out var pl) ? pl : null,
                CustomerNotes = appointment.CustomerNotes,
                StatusReason = appointment.StatusReason,
                CreatedAt = appointment.CreatedAt,
                Source = appointment.Source.ToString(),
                Notes = !string.IsNullOrEmpty(appointment.CustomerNotes) ? appointment.CustomerNotes : appointment.AdminNotes,
                Items = appointment.Items.Select(i => new AppointmentItemResponseDto
                {
                    Id = i.Id,
                    ServiceVariantId = i.ServiceVariantId,
                    CalculatedDurationMinutes = i.CalculatedDurationMinutes,
                    Price = i.Price
                }).ToList()
            };

            return new UpdatePendingFromChatResultDto
            {
                Updated = true,
                AppointmentId = appointment.Id,
                Summary = changes.Count > 0 ? string.Join(", ", changes) : "Adatok szinkronizálva",
                Appointment = responseDto,
                Parsed = parsed
            };
        }

        private static string FormatDictValues(Dictionary<string, string>? dict)
        {
            if (dict == null || dict.Count == 0) return "";
            var distinct = dict.Values
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Select(v => v.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase);
            return string.Join(" / ", distinct);
        }

        private static TimeZoneInfo? GetSalonTimeZone()
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time");
            }
            catch
            {
                try
                {
                    return TimeZoneInfo.FindSystemTimeZoneById("Europe/Bratislava");
                }
                catch
                {
                    return null;
                }
            }
        }

        private static DateTime GetSalonLocalNow()
        {
            var tz = GetSalonTimeZone();
            return tz != null
                ? TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz)
                : DateTime.UtcNow.AddHours(2);
        }

        private static string BuildSystemPrompt(
            DateTime nowLocal,
            string calendarRef,
            string catalogRef,
            string customerRef,
            string? channelHint)
        {
            return $@"You are an intelligent salon booking assistant for a hair & beauty salon (speaking Hungarian, Slovak, and English).
Analyze the selected chat messages and/or uploaded chat screenshot(s) between a client and the stylist, and assemble an appointment booking object.

CURRENT SALON DATE & TIME: {nowLocal:yyyy-MM-dd HH:mm}
UPCOMING CALENDAR REFERENCE (use this to resolve 'holnap', 'jövő kedden', 'zajtra', 'v utorok', etc.):
{calendarRef}

AVAILABLE SALON SERVICE VARIANTS (match requested treatments to the closest VariantId(s)):
{catalogRef}

EXISTING SALON CUSTOMERS (if the client's name or phone matches an existing customer, set customerId):
{customerRef}

CHANNEL HINT: {channelHint ?? "Auto-detect from context/screenshot (WhatsApp, Messenger, Viber, SMS, Email)"}

Return ONLY a valid JSON object with the following exact structure:
{{
  ""customerId"": number or null,
  ""customerName"": ""string (client's name if mentioned or visible in chat header, else empty)"",
  ""customerPhone"": ""string (client's phone number if mentioned or matched from existing customer, else empty)"",
  ""date"": ""yyyy-MM-dd or null if no date was mentioned"",
  ""time"": ""HH:mm (24h format, e.g. '14:30') or null if no time was mentioned"",
  ""serviceVariantIds"": [ array of matched integer VariantIds ],
  ""channel"": ""WhatsApp | Messenger | Viber | SMS | Email"",
  ""notes"": ""string (any extra request, hair length, color shade, or summary of the chat)"",
  ""confidencePercent"": number between 50 and 100 indicating how complete the booking data is,
  ""missingFields"": [ array of missing field names among: ""customer"", ""date"", ""time"", ""services"" ],
  ""extractedMessages"": [ array of strings representing the individual chat messages read from the screenshot or input ]
}}";
        }

        private async Task<string> CallGeminiAsync(
            List<(byte[] Bytes, string ContentType)> visualItems,
            string? messagesText,
            string apiKey,
            string systemPrompt)
        {
            var client = _httpClientFactory.CreateClient();
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key={apiKey}";

            var parts = new List<object>
            {
                new { text = systemPrompt }
            };

            if (!string.IsNullOrWhiteSpace(messagesText))
            {
                parts.Add(new { text = "Kijelölt / bemásolt üzenetek:\n" + messagesText });
            }

            foreach (var item in visualItems)
            {
                parts.Add(new
                {
                    inline_data = new
                    {
                        mime_type = item.ContentType,
                        data = Convert.ToBase64String(item.Bytes)
                    }
                });
            }

            var requestBody = new
            {
                contents = new[] { new { parts = parts.ToArray() } },
                generationConfig = new
                {
                    response_mime_type = "application/json",
                    temperature = 0.1
                }
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            var response = await client.PostAsync(url, jsonContent);
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync();
                throw new Exception($"Gemini API error ({response.StatusCode}): {err}");
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseJson);
            var root = doc.RootElement;
            if (root.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
            {
                var first = candidates[0];
                if (first.TryGetProperty("content", out var content) &&
                    content.TryGetProperty("parts", out var respParts) &&
                    respParts.GetArrayLength() > 0)
                {
                    return respParts[0].GetProperty("text").GetString() ?? "{}";
                }
            }
            return "{}";
        }

        private async Task<string> CallOpenAiAsync(
            List<(byte[] Bytes, string ContentType)> visualItems,
            string? messagesText,
            string apiKey,
            string systemPrompt)
        {
            var model = _configuration["OpenAI:VisionModel"]
                     ?? _configuration["OpenAI:Model"]
                     ?? "gpt-4o-mini";
            var client = new ChatClient(model, new ApiKeyCredential(apiKey));

            var contentParts = new List<ChatMessageContentPart>
            {
                ChatMessageContentPart.CreateTextPart(systemPrompt)
            };

            if (!string.IsNullOrWhiteSpace(messagesText))
            {
                contentParts.Add(ChatMessageContentPart.CreateTextPart("Kijelölt / bemásolt üzenetek:\n" + messagesText));
            }

            foreach (var img in visualItems)
            {
                if (img.ContentType.StartsWith("image/"))
                {
                    contentParts.Add(ChatMessageContentPart.CreateImagePart(BinaryData.FromBytes(img.Bytes), img.ContentType));
                }
            }

            var messages = new List<ChatMessage> { new UserChatMessage(contentParts) };
            var options = new ChatCompletionOptions
            {
                ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat()
            };

            var completion = await client.CompleteChatAsync(messages, options);
            return completion.Value.Content[0].Text;
        }

        private static AiChatBookingParseResultDto ParseAiJson(
            string json,
            List<(int VariantId, string Label, decimal Price, int Duration)> catalogEntries,
            dynamic customerEntries,
            DateTime nowLocal,
            string? rawText,
            string? channelHint)
        {
            var result = new AiChatBookingParseResultDto
            {
                Channel = string.IsNullOrWhiteSpace(channelHint) ? "WhatsApp" : channelHint
            };

            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("customerId", out var cidProp) && cidProp.ValueKind == JsonValueKind.Number)
                {
                    result.CustomerId = cidProp.GetInt32();
                }

                if (root.TryGetProperty("customerName", out var cnameProp) && cnameProp.ValueKind == JsonValueKind.String)
                {
                    result.CustomerName = cnameProp.GetString() ?? "";
                }

                if (root.TryGetProperty("customerPhone", out var cphoneProp) && cphoneProp.ValueKind == JsonValueKind.String)
                {
                    result.CustomerPhone = cphoneProp.GetString() ?? "";
                }

                if (root.TryGetProperty("date", out var dateProp) && dateProp.ValueKind == JsonValueKind.String)
                {
                    result.Date = dateProp.GetString();
                }

                if (root.TryGetProperty("time", out var timeProp) && timeProp.ValueKind == JsonValueKind.String)
                {
                    result.Time = timeProp.GetString();
                }

                if (root.TryGetProperty("channel", out var chProp) && chProp.ValueKind == JsonValueKind.String)
                {
                    var ch = chProp.GetString();
                    if (!string.IsNullOrWhiteSpace(ch)) result.Channel = ch;
                }

                if (root.TryGetProperty("notes", out var notesProp) && notesProp.ValueKind == JsonValueKind.String)
                {
                    result.Notes = notesProp.GetString() ?? "";
                }

                if (root.TryGetProperty("confidencePercent", out var confProp) && confProp.ValueKind == JsonValueKind.Number)
                {
                    result.ConfidencePercent = Math.Clamp(confProp.GetInt32(), 50, 100);
                }

                if (root.TryGetProperty("serviceVariantIds", out var svProp) && svProp.ValueKind == JsonValueKind.Array)
                {
                    var validIds = catalogEntries.Select(x => x.VariantId).ToHashSet();
                    foreach (var el in svProp.EnumerateArray())
                    {
                        if (el.ValueKind == JsonValueKind.Number)
                        {
                            int id = el.GetInt32();
                            if (validIds.Contains(id) && !result.ServiceVariantIds.Contains(id))
                            {
                                result.ServiceVariantIds.Add(id);
                            }
                        }
                    }
                }

                if (root.TryGetProperty("missingFields", out var mfProp) && mfProp.ValueKind == JsonValueKind.Array)
                {
                    foreach (var el in mfProp.EnumerateArray())
                    {
                        if (el.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(el.GetString()))
                        {
                            result.MissingFields.Add(el.GetString()!);
                        }
                    }
                }

                if (root.TryGetProperty("extractedMessages", out var emProp) && emProp.ValueKind == JsonValueKind.Array)
                {
                    foreach (var el in emProp.EnumerateArray())
                    {
                        if (el.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(el.GetString()))
                        {
                            result.ExtractedMessages.Add(el.GetString()!);
                        }
                    }
                }
            }
            catch
            {
                // Fallback ha a JSON nem szabályos
            }

            return result;
        }

        private static AiChatBookingParseResultDto HeuristicParse(
            string text,
            List<(int VariantId, string Label, decimal Price, int Duration)> catalog,
            List<(int Id, string FullName, string Phone)> customers,
            DateTime nowLocal,
            string? channelHint)
        {
            var res = new AiChatBookingParseResultDto
            {
                Channel = string.IsNullOrWhiteSpace(channelHint) ? "WhatsApp" : channelHint,
                Notes = text.Trim()
            };

            var lower = text.ToLowerInvariant();

            // 1. Meglévő vendég keresése név vagy telefonszám alapján
            foreach (var c in customers)
            {
                if (!string.IsNullOrWhiteSpace(c.FullName) && c.FullName.Length >= 3 &&
                    lower.Contains(c.FullName.ToLowerInvariant()))
                {
                    res.CustomerId = c.Id;
                    res.CustomerName = c.FullName;
                    res.CustomerPhone = c.Phone;
                    break;
                }
            }

            // Telefonszám kinyerése szövegből (+421..., +36..., 09..., 06...)
            var phoneMatch = Regex.Match(text, @"(\+?\d[\d\s\-]{7,14}\d)");
            if (phoneMatch.Success && string.IsNullOrWhiteSpace(res.CustomerPhone))
            {
                res.CustomerPhone = phoneMatch.Value.Trim();
            }

            // 2. Időpont (óra:perc) kinyerése (pl. 14:30, 15.00, 15 óra, o 14:00)
            var timeMatch = Regex.Match(text, @"\b([01]?\d|2[0-3])[:\.]([0-5]\d)\b");
            if (timeMatch.Success)
            {
                int h = int.Parse(timeMatch.Groups[1].Value);
                int m = int.Parse(timeMatch.Groups[2].Value);
                res.Time = $"{h:D2}:{m:D2}";
            }
            else
            {
                var hourOnlyMatch = Regex.Match(lower, @"\b([89]|1\d|20)\s*(óra|orakor|órától|hod|utol|után)\b");
                if (hourOnlyMatch.Success && int.TryParse(hourOnlyMatch.Groups[1].Value, out int hr))
                {
                    res.Time = $"{hr:D2}:00";
                }
            }

            // 3. Dátum kinyerése ("holnap", "holnapután", hét napjai, vagy yyyy-MM-dd / MM.dd)
            if (lower.Contains("holnapután") || lower.Contains("pozajtra"))
            {
                res.Date = nowLocal.Date.AddDays(2).ToString("yyyy-MM-dd");
            }
            else if (lower.Contains("holnap") || lower.Contains("zajtra") || lower.Contains("tomorrow"))
            {
                res.Date = nowLocal.Date.AddDays(1).ToString("yyyy-MM-dd");
            }
            else if (lower.Contains("ma ") || lower.Contains("dnes"))
            {
                res.Date = nowLocal.Date.ToString("yyyy-MM-dd");
            }
            else
            {
                var dayKeywords = new Dictionary<DayOfWeek, string[]>
                {
                    [DayOfWeek.Monday] = new[] { "hétfő", "hetfo", "pondelok", "monday" },
                    [DayOfWeek.Tuesday] = new[] { "kedd", "utorok", "tuesday" },
                    [DayOfWeek.Wednesday] = new[] { "szerda", "streda", "wednesday" },
                    [DayOfWeek.Thursday] = new[] { "csütörtök", "csutortok", "štvrtok", "stvrtok", "thursday" },
                    [DayOfWeek.Friday] = new[] { "péntek", "pentek", "piatok", "friday" },
                    [DayOfWeek.Saturday] = new[] { "szombat", "sobota", "saturday" },
                    [DayOfWeek.Sunday] = new[] { "vasárnap", "vasarnap", "nedeľa", "nedela", "sunday" }
                };

                foreach (var kvp in dayKeywords)
                {
                    if (kvp.Value.Any(k => lower.Contains(k)))
                    {
                        var d = nowLocal.Date.AddDays(1);
                        while (d.DayOfWeek != kvp.Key) d = d.AddDays(1);
                        res.Date = d.ToString("yyyy-MM-dd");
                        break;
                    }
                }
            }

            // 4. Szolgáltatások keresése kulcsszavak alapján
            foreach (var item in catalog)
            {
                var words = item.Label.ToLowerInvariant()
                    .Split(new[] { ' ', '-', '/', ',', '(', ')' }, StringSplitOptions.RemoveEmptyEntries)
                    .Where(w => w.Length >= 4)
                    .ToList();

                if (words.Any(w => lower.Contains(w)))
                {
                    res.ServiceVariantIds.Add(item.VariantId);
                    if (res.ServiceVariantIds.Count >= 3) break;
                }
            }

            if (res.CustomerId == null && string.IsNullOrWhiteSpace(res.CustomerName)) res.MissingFields.Add("customer");
            if (string.IsNullOrWhiteSpace(res.Date)) res.MissingFields.Add("date");
            if (string.IsNullOrWhiteSpace(res.Time)) res.MissingFields.Add("time");
            if (res.ServiceVariantIds.Count == 0) res.MissingFields.Add("services");

            res.ConfidencePercent = Math.Max(60, 100 - (res.MissingFields.Count * 15));
            return res;
        }
    }
}
