using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
    public class ChatInboxController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ITenantContext _tenantContext;
        private readonly IAiChatBookingService _aiChatBookingService;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<ChatInboxController> _logger;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        public ChatInboxController(
            AppDbContext context,
            ITenantContext tenantContext,
            IAiChatBookingService aiChatBookingService,
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory,
            ILogger<ChatInboxController> logger)
        {
            _context = context;
            _tenantContext = tenantContext;
            _aiChatBookingService = aiChatBookingService;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetInbox()
        {
            var company = _tenantContext.CurrentCompany;
            if (company == null) return NotFound(new { errorCode = "COMPANY_NOT_FOUND" });

            var dbCompany = await _context.Companies.FindAsync(company.Id);
            if (dbCompany == null) return NotFound(new { errorCode = "COMPANY_NOT_FOUND" });

            var inbox = LoadInboxFromSettings(dbCompany);

            // Korábbi teszt/szimulált minta beszélgetések automatikus kigyomlálása élesben
            int removedDemoCount = inbox.Conversations.RemoveAll(c =>
                !c.PendingAppointmentId.HasValue &&
                string.IsNullOrWhiteSpace(c.ExternalSenderId) &&
                (string.Equals(c.CustomerName, "Szabó Katalin", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(c.CustomerName, "Kovács Anna", StringComparison.OrdinalIgnoreCase)));

            bool modified = await SyncPendingAppointmentsToInboxAsync(dbCompany.Id, inbox) || removedDemoCount > 0;

            if (modified)
            {
                SaveInboxToSettings(dbCompany, inbox);
                company.SettingsJson = dbCompany.SettingsJson;
                await _context.SaveChangesAsync();
            }

            inbox.Conversations = inbox.Conversations
                .OrderByDescending(c => c.PendingAppointmentId.HasValue)
                .ThenByDescending(c => c.UpdatedAt)
                .ToList();

            return Ok(inbox);
        }

        [HttpDelete("conversations/{conversationId}")]
        [Authorize]
        public async Task<IActionResult> DeleteConversation(string conversationId)
        {
            var company = _tenantContext.CurrentCompany;
            if (company == null) return NotFound(new { errorCode = "COMPANY_NOT_FOUND" });

            var dbCompany = await _context.Companies.FindAsync(company.Id);
            if (dbCompany == null) return NotFound(new { errorCode = "COMPANY_NOT_FOUND" });

            var inbox = LoadInboxFromSettings(dbCompany);
            inbox.Conversations.RemoveAll(c => string.Equals(c.Id, conversationId, StringComparison.OrdinalIgnoreCase));

            SaveInboxToSettings(dbCompany, inbox);
            company.SettingsJson = dbCompany.SettingsJson;
            await _context.SaveChangesAsync();

            return Ok(inbox);
        }

        [HttpPut("config")]
        [Authorize]
        public async Task<IActionResult> UpdateConfig([FromBody] ChatChannelConfigDto dto)
        {
            var company = _tenantContext.CurrentCompany;
            if (company == null) return NotFound(new { errorCode = "COMPANY_NOT_FOUND" });

            var dbCompany = await _context.Companies.FindAsync(company.Id);
            if (dbCompany == null) return NotFound(new { errorCode = "COMPANY_NOT_FOUND" });

            var inbox = LoadInboxFromSettings(dbCompany);

            var cleanAlias = SanitizeAlias(string.IsNullOrWhiteSpace(dto.SalonAlias) ? BuildDefaultAlias(dbCompany) : dto.SalonAlias);
            var centralPageId = _configuration["Messenger:CentralPageId"] ?? "1455828144269648";
            inbox.Config.SalonAlias = cleanAlias;
            inbox.Config.CentralMessengerLink = $"https://m.me/{centralPageId}?ref={cleanAlias}";
            inbox.Config.CentralViberLink = $"viber://pa?chatURI=beemagicsalon&context={cleanAlias}";
            inbox.Config.CentralWhatsAppTag = $"#{cleanAlias}";

            inbox.Config.CustomFacebookPageConnected = dto.CustomFacebookPageConnected;
            inbox.Config.CustomFacebookPageName = dto.CustomFacebookPageName?.Trim();
            inbox.Config.CustomFacebookPageId = dto.CustomFacebookPageId?.Trim();
            if (!string.IsNullOrWhiteSpace(dto.CustomFacebookPageToken))
            {
                inbox.Config.CustomFacebookPageToken = dto.CustomFacebookPageToken.Trim();
            }

            inbox.Config.CustomViberBotConnected = dto.CustomViberBotConnected;
            inbox.Config.CustomViberBotName = dto.CustomViberBotName?.Trim();
            if (!string.IsNullOrWhiteSpace(dto.CustomViberBotToken))
            {
                inbox.Config.CustomViberBotToken = dto.CustomViberBotToken.Trim();
            }

            inbox.Config.IncludeCentralLinkInReplies = dto.IncludeCentralLinkInReplies;
            inbox.Config.AutoUpdatePendingFromChat = dto.AutoUpdatePendingFromChat;

            SaveInboxToSettings(dbCompany, inbox);
            company.SettingsJson = dbCompany.SettingsJson;
            await _context.SaveChangesAsync();

            return Ok(inbox.Config);
        }

        [HttpPost("conversations/{conversationId}/read")]
        [Authorize]
        public async Task<IActionResult> MarkRead(string conversationId)
        {
            var company = _tenantContext.CurrentCompany;
            if (company == null) return NotFound(new { errorCode = "COMPANY_NOT_FOUND" });

            var dbCompany = await _context.Companies.FindAsync(company.Id);
            if (dbCompany == null) return NotFound(new { errorCode = "COMPANY_NOT_FOUND" });

            var inbox = LoadInboxFromSettings(dbCompany);
            var conv = inbox.Conversations.FirstOrDefault(c => c.Id == conversationId);
            if (conv != null && conv.UnreadCount > 0)
            {
                conv.UnreadCount = 0;
                SaveInboxToSettings(dbCompany, inbox);
                company.SettingsJson = dbCompany.SettingsJson;
                await _context.SaveChangesAsync();
            }

            return Ok(new { success = true });
        }

        [HttpPost("conversations/{conversationId}/reply")]
        [Authorize]
        public async Task<IActionResult> SendReply(string conversationId, [FromBody] ChatReplyRequestDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Text))
            {
                return BadRequest(new { errorCode = "EMPTY_MESSAGE" });
            }

            var company = _tenantContext.CurrentCompany;
            if (company == null) return NotFound(new { errorCode = "COMPANY_NOT_FOUND" });

            var dbCompany = await _context.Companies.FindAsync(company.Id);
            if (dbCompany == null) return NotFound(new { errorCode = "COMPANY_NOT_FOUND" });

            var inbox = LoadInboxFromSettings(dbCompany);
            var conv = inbox.Conversations.FirstOrDefault(c => c.Id == conversationId);
            if (conv == null) return NotFound(new { errorCode = "CONVERSATION_NOT_FOUND" });

            string finalMessage = dto.Text.Trim();
            // Ha a szalon saját FB oldaláról jött az üzenet, vagy kérte a központi link hozzáfűzését ("ügyfelek átnevelése")
            if (dto.AppendCentralLink || (inbox.Config.IncludeCentralLinkInReplies && conv.SourceRoute == "custom_page"))
            {
                var linkToShare = conv.Channel == "Viber"
                    ? inbox.Config.CentralViberLink
                    : inbox.Config.CentralMessengerLink;

                if (!finalMessage.Contains(linkToShare, StringComparison.OrdinalIgnoreCase))
                {
                    finalMessage += $"\n\n💬 Közvetlen szalon-linkünk: {linkToShare}";
                }
            }

            var msg = new ChatMessageItemDto
            {
                Id = Guid.NewGuid().ToString("N"),
                Sender = "salon",
                SenderName = dbCompany.Name,
                Text = finalMessage,
                Timestamp = DateTime.UtcNow,
                SourceRoute = conv.SourceRoute
            };

            conv.Messages.Add(msg);
            conv.UnreadCount = 0;
            conv.UpdatedAt = DateTime.UtcNow;

            SaveInboxToSettings(dbCompany, inbox);
            company.SettingsJson = dbCompany.SettingsJson;
            await _context.SaveChangesAsync();

            // Ha van valódi külső csatorna token és ExternalSenderId, elküldjük a Meta / Viber API-nak is
            await TryDispatchExternalReplyAsync(inbox.Config, conv, finalMessage);

            return Ok(conv);
        }

        [HttpPost("simulate-incoming")]
        [Authorize]
        public async Task<IActionResult> SimulateIncoming([FromBody] SimulateIncomingChatDto dto)
        {
            var company = _tenantContext.CurrentCompany;
            if (company == null) return NotFound(new { errorCode = "COMPANY_NOT_FOUND" });

            var dbCompany = await _context.Companies.FindAsync(company.Id);
            if (dbCompany == null) return NotFound(new { errorCode = "COMPANY_NOT_FOUND" });

            var inbox = LoadInboxFromSettings(dbCompany);
            await SyncPendingAppointmentsToInboxAsync(dbCompany.Id, inbox);

            ChatConversationDto? targetConv = null;

            if (dto.PendingAppointmentId.HasValue)
            {
                targetConv = inbox.Conversations.FirstOrDefault(c => c.PendingAppointmentId == dto.PendingAppointmentId.Value);
            }

            if (targetConv == null && !string.IsNullOrWhiteSpace(dto.CustomerName))
            {
                targetConv = inbox.Conversations.FirstOrDefault(c =>
                    string.Equals(c.CustomerName, dto.CustomerName.Trim(), StringComparison.OrdinalIgnoreCase));
            }

            string customerName = targetConv?.CustomerName
                ?? (!string.IsNullOrWhiteSpace(dto.CustomerName) ? dto.CustomerName.Trim() : "Kovács Anna");
            string channel = !string.IsNullOrWhiteSpace(dto.Channel) ? dto.Channel : (targetConv?.Channel ?? "Messenger");
            string sourceRoute = !string.IsNullOrWhiteSpace(dto.SourceRoute) ? dto.SourceRoute : (targetConv?.SourceRoute ?? "central_alias");

            // Ha nincs megadva egyedi szöveg, generálunk egy életszerű időpont-módosító vagy új foglalási üzenetet
            string messageText = !string.IsNullOrWhiteSpace(dto.MessageText)
                ? dto.MessageText.Trim()
                : (targetConv?.PendingAppointmentId != null
                    ? "Szia! A korábban kért időpont helyett inkább 15:30-ra tudnék menni, az még szabad?"
                    : "Szia! Holnapután 14:00-kor szeretnék menni női hajvágásra és szárításra. Kovács Anna (+421 905 888 123)");

            if (targetConv == null)
            {
                targetConv = new ChatConversationDto
                {
                    Id = Guid.NewGuid().ToString("N"),
                    Channel = channel,
                    SourceRoute = sourceRoute,
                    CustomerName = customerName,
                    CustomerPhone = dto.CustomerPhone ?? "+421 905 888 123",
                    PendingAppointmentId = dto.PendingAppointmentId,
                    UnreadCount = 0,
                    UpdatedAt = DateTime.UtcNow
                };
                inbox.Conversations.Insert(0, targetConv);
            }
            else
            {
                targetConv.Channel = channel;
                targetConv.SourceRoute = sourceRoute;
            }

            targetConv.Messages.Add(new ChatMessageItemDto
            {
                Id = Guid.NewGuid().ToString("N"),
                Sender = "client",
                SenderName = targetConv.CustomerName,
                Text = messageText,
                Timestamp = DateTime.UtcNow,
                SourceRoute = sourceRoute
            });
            targetConv.UnreadCount += 1;
            targetConv.UpdatedAt = DateTime.UtcNow;

            UpdatePendingFromChatResultDto? pendingUpdateResult = null;

            // Ha a beszélgetéshez tartozik Jóváhagyásra váró (Pending) foglalás és be van kapcsolva az automatikus frissítés:
            if (targetConv.PendingAppointmentId.HasValue && inbox.Config.AutoUpdatePendingFromChat)
            {
                try
                {
                    pendingUpdateResult = await _aiChatBookingService.UpdatePendingAppointmentFromChatAsync(
                        targetConv.PendingAppointmentId.Value,
                        messageText,
                        null,
                        targetConv.Channel);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Függő foglalás automatikus chat-frissítése nem sikerült.");
                }
            }

            SaveInboxToSettings(dbCompany, inbox);
            company.SettingsJson = dbCompany.SettingsJson;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                conversation = targetConv,
                pendingUpdate = pendingUpdateResult
            });
        }

        // --- VALÓDI FACEBOOK MESSENGER WEBHOOK (Központi BeeMagic ?ref=alias ÉS Saját Facebook Oldal támogatással!) ---
        [HttpGet("webhook/messenger")]
        [AllowAnonymous]
        public IActionResult VerifyMessengerWebhook(
            [FromQuery(Name = "hub.mode")] string? mode,
            [FromQuery(Name = "hub.verify_token")] string? verifyToken,
            [FromQuery(Name = "hub.challenge")] string? challenge)
        {
            var expectedToken = _configuration["Messenger:VerifyToken"] ?? "beemagic_messenger_verify";
            if (mode == "subscribe" && verifyToken == expectedToken && !string.IsNullOrEmpty(challenge))
            {
                return Content(challenge, "text/plain");
            }
            return Forbid();
        }

        [HttpPost("webhook/messenger")]
        [AllowAnonymous]
        public async Task<IActionResult> HandleMessengerWebhook([FromBody] JsonElement payload)
        {
            try
            {
                if (!payload.TryGetProperty("entry", out var entries) || entries.ValueKind != JsonValueKind.Array)
                {
                    return Ok();
                }

                var allCompanies = await _context.Companies.IgnoreQueryFilters().Where(c => !c.IsDeleted).ToListAsync();

                foreach (var entry in entries.EnumerateArray())
                {
                    string pageId = entry.TryGetProperty("id", out var idProp) ? idProp.GetString() ?? "" : "";
                    if (!entry.TryGetProperty("messaging", out var messagingArr) || messagingArr.ValueKind != JsonValueKind.Array)
                        continue;

                    foreach (var evt in messagingArr.EnumerateArray())
                    {
                        string senderId = evt.TryGetProperty("sender", out var sProp) && sProp.TryGetProperty("id", out var sidProp)
                            ? sidProp.GetString() ?? ""
                            : "";

                        string? refAlias = null;
                        if (evt.TryGetProperty("referral", out var refObj) && refObj.TryGetProperty("ref", out var refProp))
                        {
                            refAlias = refProp.GetString();
                        }
                        else if (evt.TryGetProperty("postback", out var pbObj) &&
                                 pbObj.TryGetProperty("referral", out var pbRefObj) &&
                                 pbRefObj.TryGetProperty("ref", out var pbRefProp))
                        {
                            refAlias = pbRefProp.GetString();
                        }

                        if (evt.TryGetProperty("message", out var echoCheckObj) &&
                            echoCheckObj.TryGetProperty("is_echo", out var echoProp) &&
                            echoProp.ValueKind == JsonValueKind.True)
                        {
                            continue;
                        }

                        string text = evt.TryGetProperty("message", out var msgObj) && msgObj.TryGetProperty("text", out var txtProp)
                            ? txtProp.GetString() ?? ""
                            : "";

                        if (string.IsNullOrWhiteSpace(text) && string.IsNullOrWhiteSpace(refAlias))
                            continue;

                        // #alias keresése az üzenet szövegében is
                        if (string.IsNullOrWhiteSpace(refAlias) && !string.IsNullOrWhiteSpace(text))
                        {
                            var tagMatch = Regex.Match(text, @"#([a-zA-Z0-9_\-]+)");
                            if (tagMatch.Success) refAlias = tagMatch.Groups[1].Value;
                        }

                        // Szalon feloldása:
                        // 1) refAlias alapján (Központi BeeMagic link ?ref=alias vagy #alias)
                        // 2) CustomFacebookPageId alapján (Saját FB Oldal)
                        // 3) Korábbi beszélgetés ExternalSenderId alapján (ha a vendég már korábban rákattintott a ?ref=alias linkre!)
                        Company? matchedCompany = null;
                        string sourceRoute = "central_alias";

                        foreach (var comp in allCompanies)
                        {
                            var inbox = LoadInboxFromSettings(comp);
                            if (!string.IsNullOrWhiteSpace(refAlias) &&
                                string.Equals(inbox.Config.SalonAlias, refAlias, StringComparison.OrdinalIgnoreCase))
                            {
                                matchedCompany = comp;
                                sourceRoute = "central_alias";
                                break;
                            }

                            if (!string.IsNullOrWhiteSpace(pageId) &&
                                inbox.Config.CustomFacebookPageConnected &&
                                string.Equals(inbox.Config.CustomFacebookPageId, pageId, StringComparison.OrdinalIgnoreCase))
                            {
                                matchedCompany = comp;
                                sourceRoute = "custom_page";
                                break;
                            }
                        }

                        if (matchedCompany == null && !string.IsNullOrWhiteSpace(senderId))
                        {
                            foreach (var comp in allCompanies)
                            {
                                var inbox = LoadInboxFromSettings(comp);
                                var existingBySender = inbox.Conversations.FirstOrDefault(c =>
                                    c.Channel == "Messenger" &&
                                    string.Equals(c.ExternalSenderId, senderId, StringComparison.Ordinal));
                                if (existingBySender != null)
                                {
                                    matchedCompany = comp;
                                    sourceRoute = existingBySender.SourceRoute ?? "central_alias";
                                    break;
                                }
                            }
                        }

                        matchedCompany ??= allCompanies.FirstOrDefault();
                        if (matchedCompany == null) continue;

                        if (string.IsNullOrWhiteSpace(text) && !string.IsNullOrWhiteSpace(refAlias))
                        {
                            text = $"🔗 Vendég csatlakozott a szalon Messenger linkjén keresztül (#{refAlias})";
                        }

                        if (string.IsNullOrWhiteSpace(text)) continue;

                        string senderName = await TryResolveMessengerSenderNameAsync(matchedCompany, sourceRoute, senderId);
                        await AppendWebhookMessageAsync(matchedCompany, "Messenger", sourceRoute, senderId, senderName, text);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Messenger webhook feldolgozási hiba.");
            }

            return Ok();
        }

        // --- VALÓDI VIBER WEBHOOK (Központi BeeMagic context=alias ÉS Saját Viber Bot támogatással!) ---
        [HttpPost("webhook/viber/{salonAlias?}")]
        [AllowAnonymous]
        public async Task<IActionResult> HandleViberWebhook(string? salonAlias, [FromBody] JsonElement payload)
        {
            try
            {
                string eventType = payload.TryGetProperty("event", out var evProp) ? evProp.GetString() ?? "" : "";
                if (eventType != "message") return Ok();

                string contextAlias = salonAlias ?? "";
                if (string.IsNullOrWhiteSpace(contextAlias) && payload.TryGetProperty("context", out var ctxProp))
                {
                    contextAlias = ctxProp.GetString() ?? "";
                }

                string senderId = "";
                string senderName = "Viber Vendég";
                if (payload.TryGetProperty("sender", out var senderObj))
                {
                    if (senderObj.TryGetProperty("id", out var sidProp)) senderId = sidProp.GetString() ?? "";
                    if (senderObj.TryGetProperty("name", out var snameProp)) senderName = snameProp.GetString() ?? senderName;
                }

                string text = payload.TryGetProperty("message", out var msgObj) && msgObj.TryGetProperty("text", out var txtProp)
                    ? txtProp.GetString() ?? ""
                    : "";

                if (string.IsNullOrWhiteSpace(text)) return Ok();

                var allCompanies = await _context.Companies.IgnoreQueryFilters().Where(c => !c.IsDeleted).ToListAsync();
                Company? matchedCompany = null;
                foreach (var comp in allCompanies)
                {
                    var inbox = LoadInboxFromSettings(comp);
                    if (!string.IsNullOrWhiteSpace(contextAlias) &&
                        string.Equals(inbox.Config.SalonAlias, contextAlias, StringComparison.OrdinalIgnoreCase))
                    {
                        matchedCompany = comp;
                        break;
                    }
                }

                matchedCompany ??= allCompanies.FirstOrDefault();
                if (matchedCompany != null)
                {
                    await AppendWebhookMessageAsync(matchedCompany, "Viber", "central_alias", senderId, senderName, text);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Viber webhook feldolgozási hiba.");
            }

            return Ok();
        }

        private async Task<string> TryResolveMessengerSenderNameAsync(Company dbCompany, string sourceRoute, string senderId)
        {
            if (string.IsNullOrWhiteSpace(senderId)) return "Messenger Vendég";

            try
            {
                var inbox = LoadInboxFromSettings(dbCompany);
                var existing = inbox.Conversations.FirstOrDefault(c =>
                    c.Channel == "Messenger" &&
                    string.Equals(c.ExternalSenderId, senderId, StringComparison.Ordinal) &&
                    !string.IsNullOrWhiteSpace(c.CustomerName) &&
                    c.CustomerName != "Messenger Vendég");

                if (existing != null) return existing.CustomerName;

                var pageToken = sourceRoute == "custom_page" && !string.IsNullOrWhiteSpace(inbox.Config.CustomFacebookPageToken)
                    ? inbox.Config.CustomFacebookPageToken
                    : _configuration["Messenger:CentralPageAccessToken"];

                if (!string.IsNullOrWhiteSpace(pageToken))
                {
                    var client = _httpClientFactory.CreateClient();
                    var resp = await client.GetAsync($"https://graph.facebook.com/v20.0/{senderId}?fields=first_name,last_name&access_token={pageToken}");
                    if (resp.IsSuccessStatusCode)
                    {
                        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
                        var root = doc.RootElement;
                        var first = root.TryGetProperty("first_name", out var fp) ? fp.GetString() ?? "" : "";
                        var last = root.TryGetProperty("last_name", out var lp) ? lp.GetString() ?? "" : "";
                        var full = $"{last} {first}".Trim();
                        if (!string.IsNullOrWhiteSpace(full)) return full;
                    }
                }
            }
            catch
            {
                // Fallback ha a profil lekérdezés nem engedélyezett
            }

            return "Messenger Vendég";
        }

        private async Task AppendWebhookMessageAsync(
            Company dbCompany,
            string channel,
            string sourceRoute,
            string externalSenderId,
            string senderName,
            string text)
        {
            _tenantContext.CurrentCompany = dbCompany;
            var inbox = LoadInboxFromSettings(dbCompany);
            await SyncPendingAppointmentsToInboxAsync(dbCompany.Id, inbox);

            var conv = inbox.Conversations.FirstOrDefault(c =>
                (!string.IsNullOrWhiteSpace(externalSenderId) && c.ExternalSenderId == externalSenderId && c.Channel == channel) ||
                (!string.IsNullOrWhiteSpace(senderName) && senderName != "Messenger Vendég" && senderName != "Viber Vendég" &&
                 string.Equals(c.CustomerName, senderName, StringComparison.OrdinalIgnoreCase)));

            // Ha még nincs összekötve beszélgetés ezzel a külső küldővel, de van aktív Jóváhagyásra váró (Pending) foglalás, összekapcsoljuk a legfrissebbel
            if (conv == null)
            {
                conv = inbox.Conversations.FirstOrDefault(c =>
                    c.PendingAppointmentId.HasValue &&
                    string.IsNullOrWhiteSpace(c.ExternalSenderId) &&
                    string.Equals(c.Channel, channel, StringComparison.OrdinalIgnoreCase));
            }

            if (conv == null)
            {
                conv = new ChatConversationDto
                {
                    Id = Guid.NewGuid().ToString("N"),
                    Channel = channel,
                    SourceRoute = sourceRoute,
                    ExternalSenderId = externalSenderId,
                    CustomerName = senderName,
                    UnreadCount = 0,
                    UpdatedAt = DateTime.UtcNow
                };
                inbox.Conversations.Insert(0, conv);
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(externalSenderId))
                {
                    conv.ExternalSenderId = externalSenderId;
                }
                if (!string.IsNullOrWhiteSpace(senderName) &&
                    senderName != "Messenger Vendég" &&
                    senderName != "Viber Vendég" &&
                    (string.IsNullOrWhiteSpace(conv.CustomerName) || conv.CustomerName == "Messenger Vendég" || conv.CustomerName == "Viber Vendég"))
                {
                    conv.CustomerName = senderName;
                }
                conv.Channel = channel;
                conv.SourceRoute = sourceRoute;
            }

            conv.Messages.Add(new ChatMessageItemDto
            {
                Id = Guid.NewGuid().ToString("N"),
                Sender = "client",
                SenderName = conv.CustomerName,
                Text = text,
                Timestamp = DateTime.UtcNow,
                SourceRoute = sourceRoute
            });
            conv.UnreadCount += 1;
            conv.UpdatedAt = DateTime.UtcNow;

            if (conv.PendingAppointmentId.HasValue && inbox.Config.AutoUpdatePendingFromChat && !text.StartsWith("🔗"))
            {
                try
                {
                    await _aiChatBookingService.UpdatePendingAppointmentFromChatAsync(
                        conv.PendingAppointmentId.Value,
                        text,
                        null,
                        conv.Channel);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Valódi webhook üzenetből függő foglalás automatikus frissítése nem sikerült.");
                }
            }

            SaveInboxToSettings(dbCompany, inbox);
            await _context.SaveChangesAsync();
        }

        private async Task<bool> SyncPendingAppointmentsToInboxAsync(int companyId, ChatInboxResponseDto inbox)
        {
            bool changed = false;

            var pendingApps = await _context.Appointments
                .AsNoTracking()
                .Include(a => a.Customer)
                .Include(a => a.Items)
                .Where(a => a.CompanyId == companyId && a.Status == AppointmentStatus.Pending)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            var activePendingIds = pendingApps.Select(a => a.Id).ToHashSet();

            // Ha egy korábban függő foglalást már jóváhagytak vagy elutasítottak, levesszük róla a pending jelzőt
            foreach (var conv in inbox.Conversations)
            {
                if (conv.PendingAppointmentId.HasValue && !activePendingIds.Contains(conv.PendingAppointmentId.Value))
                {
                    conv.PendingAppointmentId = null;
                    changed = true;
                }
            }

            // Minden aktív Jóváhagyásra váró foglaláshoz biztosítunk egy élő beszélgetést a Chat Inboxban
            foreach (var app in pendingApps)
            {
                var existingConv = inbox.Conversations.FirstOrDefault(c => c.PendingAppointmentId == app.Id);
                string custName = app.Customer?.Attributes != null && app.Customer.Attributes.TryGetValue("FullName", out var fn) && !string.IsNullOrWhiteSpace(fn)
                    ? fn
                    : $"Vendég #{app.CustomerId}";
                string? custPhone = app.Customer?.Attributes != null && app.Customer.Attributes.TryGetValue("Phone", out var ph) ? ph : null;
                string? custEmail = app.Customer?.Attributes != null && app.Customer.Attributes.TryGetValue("Email", out var em) ? em : null;
                string prefChannel = app.Customer?.Attributes != null && app.Customer.Attributes.TryGetValue("PreferredChannel", out var pc) && !string.IsNullOrWhiteSpace(pc)
                    ? pc
                    : "WhatsApp";

                if (existingConv == null)
                {
                    var localStart = app.StartDateTime.AddHours(2);
                    var initialText = $"📅 Online időpontkérés: {localStart:yyyy.MM.dd. HH:mm} ({app.TotalPrice} EUR)" +
                        (!string.IsNullOrWhiteSpace(app.CustomerNotes) ? $" – Megjegyzés: {app.CustomerNotes}" : "");

                    var newConv = new ChatConversationDto
                    {
                        Id = $"pending-{app.Id}",
                        Channel = prefChannel,
                        SourceRoute = "central_alias",
                        CustomerId = app.CustomerId,
                        CustomerName = custName,
                        CustomerPhone = custPhone,
                        CustomerEmail = custEmail,
                        PendingAppointmentId = app.Id,
                        UnreadCount = 0,
                        UpdatedAt = app.CreatedAt,
                        Messages = new List<ChatMessageItemDto>
                        {
                            new ChatMessageItemDto
                            {
                                Id = $"init-{app.Id}",
                                Sender = "client",
                                SenderName = custName,
                                Text = initialText,
                                Timestamp = app.CreatedAt,
                                SourceRoute = "booking"
                            }
                        }
                    };

                    inbox.Conversations.Add(newConv);
                    changed = true;
                }
                else
                {
                    if (existingConv.CustomerName != custName)
                    {
                        existingConv.CustomerName = custName;
                        changed = true;
                    }
                    if (existingConv.CustomerPhone != custPhone)
                    {
                        existingConv.CustomerPhone = custPhone;
                        changed = true;
                    }
                }
            }

            return changed;
        }

        private ChatInboxResponseDto LoadInboxFromSettings(Company company)
        {
            var defaultAlias = BuildDefaultAlias(company);
            var centralPageId = _configuration["Messenger:CentralPageId"] ?? "1455828144269648";
            var response = new ChatInboxResponseDto
            {
                Config = new ChatChannelConfigDto
                {
                    SalonAlias = defaultAlias,
                    CentralMessengerLink = $"https://m.me/{centralPageId}?ref={defaultAlias}",
                    CentralViberLink = $"viber://pa?chatURI=beemagicsalon&context={defaultAlias}",
                    CentralWhatsAppTag = $"#{defaultAlias}",
                    IncludeCentralLinkInReplies = true,
                    AutoUpdatePendingFromChat = true
                }
            };

            if (!string.IsNullOrWhiteSpace(company.SettingsJson))
            {
                try
                {
                    var root = JsonNode.Parse(company.SettingsJson) as JsonObject;
                    if (root != null && root["chatInbox"] != null)
                    {
                        var parsed = root["chatInbox"].Deserialize<ChatInboxResponseDto>(JsonOptions);
                        if (parsed != null)
                        {
                            response = parsed;
                        }
                    }
                }
                catch
                {
                    // Fallback az alapértelmezett inboxra
                }
            }

            if (string.IsNullOrWhiteSpace(response.Config.SalonAlias))
            {
                response.Config.SalonAlias = defaultAlias;
            }
            response.Config.CentralMessengerLink = $"https://m.me/{centralPageId}?ref={response.Config.SalonAlias}";
            response.Config.CentralViberLink = $"viber://pa?chatURI=beemagicsalon&context={response.Config.SalonAlias}";
            response.Config.CentralWhatsAppTag = $"#{response.Config.SalonAlias}";

            return response;
        }

        private static void SaveInboxToSettings(Company company, ChatInboxResponseDto inbox)
        {
            JsonObject root;
            try
            {
                root = (!string.IsNullOrWhiteSpace(company.SettingsJson)
                    ? JsonNode.Parse(company.SettingsJson) as JsonObject
                    : null) ?? new JsonObject();
            }
            catch
            {
                root = new JsonObject();
            }

            // Legfeljebb 40 beszélgetést és beszélgetésenként 40 üzenetet tartunk meg a gyors működésért
            inbox.Conversations = inbox.Conversations
                .OrderByDescending(c => c.PendingAppointmentId.HasValue)
                .ThenByDescending(c => c.UpdatedAt)
                .Take(40)
                .ToList();

            foreach (var c in inbox.Conversations)
            {
                if (c.Messages.Count > 40)
                {
                    c.Messages = c.Messages.Skip(c.Messages.Count - 40).ToList();
                }
            }

            root["chatInbox"] = JsonSerializer.SerializeToNode(inbox, JsonOptions);
            company.SettingsJson = root.ToJsonString();
        }

        private static string BuildDefaultAlias(Company company)
        {
            var raw = !string.IsNullOrWhiteSpace(company.Domain)
                ? company.Domain.Split('.')[0]
                : company.Name;
            return SanitizeAlias(raw);
        }

        private static string SanitizeAlias(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "salon";
            var normalized = input.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (var ch in normalized)
            {
                var cat = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (cat == UnicodeCategory.NonSpacingMark) continue;
                if ((ch >= 'a' && ch <= 'z') || (ch >= '0' && ch <= '9') || ch == '-' || ch == '_')
                {
                    sb.Append(ch);
                }
            }
            return sb.Length > 0 ? sb.ToString() : "salon";
        }

        private async Task TryDispatchExternalReplyAsync(ChatChannelConfigDto config, ChatConversationDto conv, string text)
        {
            if (string.IsNullOrWhiteSpace(conv.ExternalSenderId)) return;

            try
            {
                var client = _httpClientFactory.CreateClient();

                if (conv.Channel == "Messenger")
                {
                    var pageToken = conv.SourceRoute == "custom_page" && !string.IsNullOrWhiteSpace(config.CustomFacebookPageToken)
                        ? config.CustomFacebookPageToken
                        : _configuration["Messenger:CentralPageAccessToken"];

                    if (!string.IsNullOrWhiteSpace(pageToken))
                    {
                        var url = $"https://graph.facebook.com/v20.0/me/messages?access_token={pageToken}";
                        var payload = new
                        {
                            recipient = new { id = conv.ExternalSenderId },
                            message = new { text }
                        };
                        await client.PostAsync(url, new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));
                    }
                }
                else if (conv.Channel == "Viber")
                {
                    var viberToken = conv.SourceRoute == "custom_page" && !string.IsNullOrWhiteSpace(config.CustomViberBotToken)
                        ? config.CustomViberBotToken
                        : _configuration["Viber:CentralAuthToken"];

                    if (!string.IsNullOrWhiteSpace(viberToken))
                    {
                        using var req = new HttpRequestMessage(HttpMethod.Post, "https://chatapi.viber.com/pa/send_message");
                        req.Headers.Add("X-Viber-Auth-Token", viberToken);
                        var payload = new
                        {
                            receiver = conv.ExternalSenderId,
                            type = "text",
                            text
                        };
                        req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                        await client.SendAsync(req);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Külső Messenger/Viber API üzenetküldés nem sikerült.");
            }
        }
    }
}
