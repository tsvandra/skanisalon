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

        private static readonly List<object> RecentWebhookEvents = new();

        [HttpGet("webhook-status")]
        [AllowAnonymous]
        public async Task<IActionResult> GetWebhookStatus()
        {
            var companies = await _context.Companies.IgnoreQueryFilters().Where(c => !c.IsDeleted).ToListAsync();
            var summary = companies.Select(c =>
            {
                var ib = LoadInboxFromSettings(c);
                return new
                {
                    c.Id,
                    c.Name,
                    c.Domain,
                    Alias = ib.Config.SalonAlias,
                    ConversationCount = ib.Conversations.Count,
                    Conversations = ib.Conversations.Select(conv => new
                    {
                        conv.Id,
                        conv.Channel,
                        conv.SourceRoute,
                        conv.CustomerName,
                        conv.ExternalSenderId,
                        conv.UpdatedAt,
                        MessageCount = conv.Messages.Count,
                        LastMessage = conv.Messages.LastOrDefault()?.Text,
                        Messages = conv.Messages
                    })
                };
            });

            return Ok(new
            {
                recentWebhooks = RecentWebhookEvents.TakeLast(15),
                companies = summary
            });
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

            // Ha egy valódi webhook üzenet korábban az 1-es (teszt) cégre futott be alias eltérés miatt, átmentjük az aktuális szalonra
            bool rescuedFromOtherCompany = false;
            var otherCompanies = await _context.Companies
                .IgnoreQueryFilters()
                .Where(c => !c.IsDeleted && c.Id != dbCompany.Id)
                .ToListAsync();

            foreach (var otherComp in otherCompanies)
            {
                var otherInbox = LoadInboxFromSettings(otherComp);
                var webhookConvs = otherInbox.Conversations
                    .Where(c => !string.IsNullOrWhiteSpace(c.ExternalSenderId))
                    .ToList();

                if (webhookConvs.Count > 0 && (dbCompany.Id == 7 || otherComp.Id == 1))
                {
                    foreach (var wc in webhookConvs)
                    {
                        var existing = inbox.Conversations.FirstOrDefault(c =>
                            c.Channel == wc.Channel &&
                            string.Equals(c.ExternalSenderId, wc.ExternalSenderId, StringComparison.Ordinal));
                        if (existing == null)
                        {
                            inbox.Conversations.Insert(0, wc);
                        }
                        else
                        {
                            foreach (var m in wc.Messages)
                            {
                                if (!existing.Messages.Any(em => em.Id == m.Id || (em.Text == m.Text && Math.Abs((em.Timestamp - m.Timestamp).TotalSeconds) < 5)))
                                {
                                    existing.Messages.Add(m);
                                }
                            }
                        }
                        otherInbox.Conversations.Remove(wc);
                    }
                    SaveInboxToSettings(otherComp, otherInbox);
                    rescuedFromOtherCompany = true;
                }
            }

            bool fbSynced = await SyncFacebookPageConversationsAsync(dbCompany, inbox);
            bool modified = await SyncPendingAppointmentsToInboxAsync(dbCompany.Id, inbox) || removedDemoCount > 0 || rescuedFromOtherCompany || fbSynced;

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
            inbox.Config.ClearedSenderTimestamps ??= new Dictionary<string, DateTime>();

            var toRemove = inbox.Conversations
                .Where(c => string.Equals(c.Id, conversationId, StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var conv in toRemove)
            {
                var watermark = DateTime.UtcNow;
                if (conv.Messages.Count > 0)
                {
                    var maxMsgTs = conv.Messages.Max(m => m.Timestamp);
                    if (maxMsgTs > watermark) watermark = maxMsgTs;
                }

                if (!string.IsNullOrWhiteSpace(conv.ExternalSenderId))
                {
                    inbox.Config.ClearedSenderTimestamps[conv.ExternalSenderId.Trim()] = watermark;
                }

                if (!string.IsNullOrWhiteSpace(conv.CustomerName) &&
                    !string.Equals(conv.CustomerName, "Messenger Vendég", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(conv.CustomerName, "Viber Vendég", StringComparison.OrdinalIgnoreCase))
                {
                    inbox.Config.ClearedSenderTimestamps[conv.CustomerName.Trim().ToLowerInvariant()] = watermark;
                }
            }

            inbox.Conversations.RemoveAll(c => string.Equals(c.Id, conversationId, StringComparison.OrdinalIgnoreCase));

            SaveInboxToSettings(dbCompany, inbox);
            company.SettingsJson = dbCompany.SettingsJson;
            await _context.SaveChangesAsync();

            return Ok(inbox);
        }

        [HttpPost("conversations/{conversationId}/mark-processed")]
        [Authorize]
        public async Task<IActionResult> MarkConversationProcessed(string conversationId, [FromBody] MarkConversationProcessedRequestDto? dto)
        {
            var company = _tenantContext.CurrentCompany;
            if (company == null) return NotFound(new { errorCode = "COMPANY_NOT_FOUND" });

            var dbCompany = await _context.Companies.FindAsync(company.Id);
            if (dbCompany == null) return NotFound(new { errorCode = "COMPANY_NOT_FOUND" });

            var inbox = LoadInboxFromSettings(dbCompany);
            var conv = inbox.Conversations.FirstOrDefault(c => string.Equals(c.Id, conversationId, StringComparison.OrdinalIgnoreCase));
            if (conv == null) return NotFound(new { errorCode = "CONVERSATION_NOT_FOUND" });

            var targetIds = dto?.MessageIds != null && dto.MessageIds.Count > 0
                ? dto.MessageIds.ToHashSet(StringComparer.OrdinalIgnoreCase)
                : null;

            DateTime maxMarkedTimestamp = DateTime.UtcNow;
            foreach (var msg in conv.Messages)
            {
                if (targetIds == null || targetIds.Contains(msg.Id))
                {
                    msg.IsProcessed = true;
                    if (msg.Timestamp > maxMarkedTimestamp)
                    {
                        maxMarkedTimestamp = msg.Timestamp;
                    }
                }
            }

            // Ha minden üzenet fel lett dolgozva (vagy nem adtak meg részlistát), frissítjük a beszélgetés és a küldő vízjelét is
            bool allClientMessagesProcessed = conv.Messages.Where(m => m.Sender == "client").All(m => m.IsProcessed);
            if (targetIds == null || allClientMessagesProcessed)
            {
                conv.LastProcessedAt = maxMarkedTimestamp;
                inbox.Config.ProcessedSenderTimestamps ??= new Dictionary<string, DateTime>();

                if (!string.IsNullOrWhiteSpace(conv.ExternalSenderId))
                {
                    inbox.Config.ProcessedSenderTimestamps[conv.ExternalSenderId.Trim()] = maxMarkedTimestamp;
                }
                if (!string.IsNullOrWhiteSpace(conv.CustomerName) &&
                    !string.Equals(conv.CustomerName, "Messenger Vendég", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(conv.CustomerName, "Viber Vendég", StringComparison.OrdinalIgnoreCase))
                {
                    inbox.Config.ProcessedSenderTimestamps[conv.CustomerName.Trim().ToLowerInvariant()] = maxMarkedTimestamp;
                }
            }

            conv.UnreadCount = 0;

            SaveInboxToSettings(dbCompany, inbox);
            company.SettingsJson = dbCompany.SettingsJson;
            await _context.SaveChangesAsync();

            return Ok(conv);
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
            if (string.Equals(cleanAlias, "szkaniszalon", StringComparison.OrdinalIgnoreCase))
            {
                cleanAlias = "skanisalon";
            }

            var centralPageId = _configuration["Messenger:CentralPageId"] ?? "1455828144269648";
            inbox.Config.SalonAlias = cleanAlias;

            inbox.Config.CustomFacebookPageConnected = dto.CustomFacebookPageConnected;
            inbox.Config.CustomFacebookPageName = dto.CustomFacebookPageName?.Trim();
            inbox.Config.CustomFacebookPageId = dto.CustomFacebookPageId?.Trim();
            if (!string.IsNullOrWhiteSpace(dto.CustomFacebookPageToken))
            {
                inbox.Config.CustomFacebookPageToken = dto.CustomFacebookPageToken.Trim();
            }

            var activePageId = (inbox.Config.CustomFacebookPageConnected && !string.IsNullOrWhiteSpace(inbox.Config.CustomFacebookPageId))
                ? inbox.Config.CustomFacebookPageId
                : centralPageId;

            inbox.Config.CentralMessengerLink = $"https://m.me/{activePageId}?ref={cleanAlias}";
            inbox.Config.CentralViberLink = $"viber://pa?chatURI=beemagicsalon&context={cleanAlias}";
            inbox.Config.CentralWhatsAppTag = $"#{cleanAlias}";

            inbox.Config.CustomViberBotConnected = dto.CustomViberBotConnected;
            inbox.Config.CustomViberBotName = dto.CustomViberBotName?.Trim();
            if (!string.IsNullOrWhiteSpace(dto.CustomViberBotToken))
            {
                inbox.Config.CustomViberBotToken = dto.CustomViberBotToken.Trim();
            }

            inbox.Config.IncludeCentralLinkInReplies = dto.IncludeCentralLinkInReplies;
            inbox.Config.AutoUpdatePendingFromChat = dto.AutoUpdatePendingFromChat;

            if (inbox.Config.CustomFacebookPageConnected &&
                !string.IsNullOrWhiteSpace(inbox.Config.CustomFacebookPageId) &&
                !string.IsNullOrWhiteSpace(inbox.Config.CustomFacebookPageToken))
            {
                await TrySubscribePageWebhookAsync(inbox.Config.CustomFacebookPageId, inbox.Config.CustomFacebookPageToken);
            }

            SaveInboxToSettings(dbCompany, inbox);
            company.SettingsJson = dbCompany.SettingsJson;
            await _context.SaveChangesAsync();

            return Ok(inbox.Config);
        }

        // --- 1-KATTINTÁSOS SAJÁT FACEBOOK OLDAL ÖSSZEKÖTŐ (OAuth Popup / Token alapján) ---
        [HttpPost("facebook/pages")]
        [Authorize]
        public async Task<IActionResult> GetFacebookManagedPages([FromBody] FacebookUserTokenRequestDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.UserAccessToken))
            {
                return BadRequest(new { errorCode = "MISSING_FB_TOKEN" });
            }

            var token = dto.UserAccessToken.Trim();
            var pages = new List<FacebookPageItemDto>();
            var client = _httpClientFactory.CreateClient();

            try
            {
                // 1. Lekérdezzük a felhasználó által kezelt összes Facebook Oldalt (User Access Token esetén)
                var resp = await client.GetAsync($"https://graph.facebook.com/v20.0/me/accounts?fields=id,name,access_token,picture{{url}}&access_token={token}");
                if (resp.IsSuccessStatusCode)
                {
                    using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
                    if (doc.RootElement.TryGetProperty("data", out var dataArr) && dataArr.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in dataArr.EnumerateArray())
                        {
                            var pid = item.TryGetProperty("id", out var idp) ? idp.GetString() ?? "" : "";
                            var pname = item.TryGetProperty("name", out var np) ? np.GetString() ?? "" : "";
                            var ptok = item.TryGetProperty("access_token", out var tp) ? tp.GetString() ?? "" : "";
                            string? picUrl = null;
                            if (item.TryGetProperty("picture", out var picObj) &&
                                picObj.TryGetProperty("data", out var picData) &&
                                picData.TryGetProperty("url", out var urlProp))
                            {
                                picUrl = urlProp.GetString();
                            }

                            if (!string.IsNullOrWhiteSpace(pid) && !string.IsNullOrWhiteSpace(ptok))
                            {
                                pages.Add(new FacebookPageItemDto
                                {
                                    Id = pid,
                                    Name = pname,
                                    AccessToken = ptok,
                                    PictureUrl = picUrl
                                });
                            }
                        }
                    }
                }

                // 2. Ha közvetlenül egy Page Access Tokent adtak meg, a /me végpont maga a Facebook Oldal!
                if (pages.Count == 0)
                {
                    var meResp = await client.GetAsync($"https://graph.facebook.com/v20.0/me?fields=id,name,picture{{url}}&access_token={token}");
                    if (meResp.IsSuccessStatusCode)
                    {
                        using var meDoc = JsonDocument.Parse(await meResp.Content.ReadAsStringAsync());
                        var root = meDoc.RootElement;
                        var pid = root.TryGetProperty("id", out var idp) ? idp.GetString() ?? "" : "";
                        var pname = root.TryGetProperty("name", out var np) ? np.GetString() ?? "" : "";
                        string? picUrl = null;
                        if (root.TryGetProperty("picture", out var picObj) &&
                            picObj.TryGetProperty("data", out var picData) &&
                            picData.TryGetProperty("url", out var urlProp))
                        {
                            picUrl = urlProp.GetString();
                        }

                        if (!string.IsNullOrWhiteSpace(pid))
                        {
                            pages.Add(new FacebookPageItemDto
                            {
                                Id = pid,
                                Name = pname,
                                AccessToken = token,
                                PictureUrl = picUrl
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Facebook oldalak lekérdezése sikertelen.");
            }

            return Ok(new { pages });
        }

        [HttpPost("facebook/connect-page")]
        [Authorize]
        public async Task<IActionResult> ConnectFacebookPage([FromBody] FacebookConnectPageRequestDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.PageId) || string.IsNullOrWhiteSpace(dto.PageAccessToken))
            {
                return BadRequest(new { errorCode = "INVALID_PAGE_DATA" });
            }

            var company = _tenantContext.CurrentCompany;
            if (company == null) return NotFound(new { errorCode = "COMPANY_NOT_FOUND" });

            var dbCompany = await _context.Companies.FindAsync(company.Id);
            if (dbCompany == null) return NotFound(new { errorCode = "COMPANY_NOT_FOUND" });

            var inbox = LoadInboxFromSettings(dbCompany);
            var cleanPageId = dto.PageId.Trim();
            var cleanPageName = string.IsNullOrWhiteSpace(dto.PageName) ? dbCompany.Name : dto.PageName.Trim();
            var cleanToken = dto.PageAccessToken.Trim();

            inbox.Config.CustomFacebookPageConnected = true;
            inbox.Config.CustomFacebookPageId = cleanPageId;
            inbox.Config.CustomFacebookPageName = cleanPageName;
            inbox.Config.CustomFacebookPageToken = cleanToken;
            inbox.Config.CentralMessengerLink = $"https://m.me/{cleanPageId}?ref={inbox.Config.SalonAlias}";

            await TrySubscribePageWebhookAsync(cleanPageId, cleanToken);
            await SyncFacebookPageConversationsAsync(dbCompany, inbox);

            SaveInboxToSettings(dbCompany, inbox);
            company.SettingsJson = dbCompany.SettingsJson;
            await _context.SaveChangesAsync();

            return Ok(inbox);
        }

        private async Task TrySubscribePageWebhookAsync(string pageId, string pageAccessToken)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var url = $"https://graph.facebook.com/v20.0/{pageId}/subscribed_apps?subscribed_fields=messages,messaging_postbacks,messaging_referrals&access_token={pageAccessToken}";
                await client.PostAsync(url, new StringContent("", Encoding.UTF8, "application/json"));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Facebook Page webhook feliratkozás sikertelen: {PageId}", pageId);
            }
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
                            if (!string.IsNullOrWhiteSpace(refAlias) && IsAliasMatchForCompany(comp, inbox, refAlias))
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

                        // Fallback: DefaultTenantId vagy Szkáni Szalon (Id == 7), végül az első aktív cég
                        var defaultTenantId = _configuration.GetValue<int?>("AppSettings:DefaultTenantId") ?? 7;
                        matchedCompany ??= allCompanies.FirstOrDefault(c => c.Id == defaultTenantId)
                            ?? allCompanies.FirstOrDefault(c => c.Id == 7 || (c.Name != null && c.Name.Contains("Szkáni", StringComparison.OrdinalIgnoreCase)))
                            ?? allCompanies.FirstOrDefault();

                        if (matchedCompany == null) continue;

                        if (string.IsNullOrWhiteSpace(text) && !string.IsNullOrWhiteSpace(refAlias))
                        {
                            text = $"🔗 Vendég csatlakozott a szalon Messenger linkjén keresztül (#{refAlias})";
                        }

                        if (string.IsNullOrWhiteSpace(text)) continue;

                        RecentWebhookEvents.Add(new
                        {
                            Timestamp = DateTime.UtcNow,
                            PageId = pageId,
                            SenderId = senderId,
                            RefAlias = refAlias,
                            Text = text,
                            MatchedCompanyId = matchedCompany.Id,
                            MatchedCompanyName = matchedCompany.Name
                        });
                        if (RecentWebhookEvents.Count > 30) RecentWebhookEvents.RemoveAt(0);

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

            if (string.IsNullOrWhiteSpace(response.Config.SalonAlias) ||
                string.Equals(response.Config.SalonAlias, "szkaniszalon", StringComparison.OrdinalIgnoreCase))
            {
                response.Config.SalonAlias = defaultAlias;
            }
            var effectivePageId = (response.Config.CustomFacebookPageConnected && !string.IsNullOrWhiteSpace(response.Config.CustomFacebookPageId))
                ? response.Config.CustomFacebookPageId.Trim()
                : centralPageId;
            response.Config.CentralMessengerLink = $"https://m.me/{effectivePageId}?ref={response.Config.SalonAlias}";
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

        private static bool IsAliasMatchForCompany(Company company, ChatInboxResponseDto inbox, string refAlias)
        {
            var cleanRef = SanitizeAlias(refAlias);
            if (string.IsNullOrWhiteSpace(cleanRef)) return false;

            if (string.Equals(SanitizeAlias(inbox.Config.SalonAlias), cleanRef, StringComparison.OrdinalIgnoreCase))
                return true;

            if (string.Equals(BuildDefaultAlias(company), cleanRef, StringComparison.OrdinalIgnoreCase))
                return true;

            if (string.Equals(SanitizeAlias(company.Name), cleanRef, StringComparison.OrdinalIgnoreCase))
                return true;

            if ((company.Id == 7 || (company.Name != null && company.Name.Contains("Szkáni", StringComparison.OrdinalIgnoreCase))) &&
                (cleanRef == "skanisalon" || cleanRef == "szkaniszalon" || cleanRef == "skani" || cleanRef == "szkani"))
            {
                return true;
            }

            return false;
        }

        private static string BuildDefaultAlias(Company company)
        {
            if (company.Id == 7 || (company.Name != null && company.Name.Contains("Szkáni", StringComparison.OrdinalIgnoreCase)))
            {
                return "skanisalon";
            }

            if (!string.IsNullOrWhiteSpace(company.Domain))
            {
                var d = company.Domain.Trim().ToLowerInvariant();
                if (d.StartsWith("www.")) d = d.Substring(4);
                var firstPart = d.Split('.')[0];
                if (!string.IsNullOrWhiteSpace(firstPart) && firstPart != "localhost")
                {
                    return SanitizeAlias(firstPart);
                }
            }

            return SanitizeAlias(company.Name);
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

        // Közvetlen szinkronizálás a Facebook Page Conversations Graph API-ból (hogy a webhook előtti vagy késleltetett üzenetek is azonnal megjelenjenek!)
        private async Task<bool> SyncFacebookPageConversationsAsync(Company dbCompany, ChatInboxResponseDto inbox)
        {
            var tokensToPoll = new List<(string Token, string Route)>();

            var centralToken = _configuration["Messenger:CentralPageAccessToken"];
            if (!string.IsNullOrWhiteSpace(centralToken))
            {
                tokensToPoll.Add((centralToken.Trim(), "central_alias"));
            }

            if (inbox.Config.CustomFacebookPageConnected && !string.IsNullOrWhiteSpace(inbox.Config.CustomFacebookPageToken))
            {
                tokensToPoll.Add((inbox.Config.CustomFacebookPageToken.Trim(), "custom_page"));
            }

            // Ha localhoston futunk és nincs helyi FB token beállítva, lehúzzuk az éles BeeMagic szerverről a szinkronizált üzeneteket!
            if (tokensToPoll.Count == 0)
            {
                return await TrySyncFromProductionBridgeAsync(dbCompany, inbox);
            }

            bool anyModified = false;
            var client = _httpClientFactory.CreateClient();
            var centralPageId = _configuration["Messenger:CentralPageId"] ?? "1455828144269648";

            foreach (var (token, route) in tokensToPoll)
            {
                try
                {
                    var url = $"https://graph.facebook.com/v20.0/me/conversations?fields=participants,updated_time,messages.limit(15){{id,message,from,created_time}}&access_token={token}";
                    var resp = await client.GetAsync(url);
                    if (!resp.IsSuccessStatusCode) continue;

                    using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
                    if (!doc.RootElement.TryGetProperty("data", out var threads) || threads.ValueKind != JsonValueKind.Array)
                        continue;

                    foreach (var thread in threads.EnumerateArray())
                    {
                        string clientSenderId = "";
                        string clientName = "Messenger Vendég";

                        if (thread.TryGetProperty("participants", out var partObj) &&
                            partObj.TryGetProperty("data", out var partArr) &&
                            partArr.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var p in partArr.EnumerateArray())
                            {
                                var pid = p.TryGetProperty("id", out var pidProp) ? pidProp.GetString() ?? "" : "";
                                var pname = p.TryGetProperty("name", out var pnameProp) ? pnameProp.GetString() ?? "" : "";
                                if (!string.IsNullOrWhiteSpace(pid) &&
                                    pid != centralPageId &&
                                    pid != inbox.Config.CustomFacebookPageId &&
                                    !string.Equals(pname, "BeeMagic", StringComparison.OrdinalIgnoreCase))
                                {
                                    clientSenderId = pid;
                                    if (!string.IsNullOrWhiteSpace(pname)) clientName = pname;
                                    break;
                                }
                            }
                        }

                        if (!thread.TryGetProperty("messages", out var msgsWrapper) ||
                            !msgsWrapper.TryGetProperty("data", out var msgsArr) ||
                            msgsArr.ValueKind != JsonValueKind.Array)
                        {
                            continue;
                        }

                        var rawMessages = new List<(string Id, string Text, string FromId, string FromName, DateTime CreatedAt)>();
                        foreach (var m in msgsArr.EnumerateArray())
                        {
                            var mid = m.TryGetProperty("id", out var midProp) ? midProp.GetString() ?? "" : "";
                            var mtext = m.TryGetProperty("message", out var mtProp) ? mtProp.GetString() ?? "" : "";
                            if (string.IsNullOrWhiteSpace(mtext)) continue;

                            string fromId = "";
                            string fromName = "";
                            if (m.TryGetProperty("from", out var fromObj))
                            {
                                if (fromObj.TryGetProperty("id", out var fidProp)) fromId = fidProp.GetString() ?? "";
                                if (fromObj.TryGetProperty("name", out var fnProp)) fromName = fnProp.GetString() ?? "";
                            }

                            DateTime created = DateTime.UtcNow;
                            if (m.TryGetProperty("created_time", out var ctProp) && DateTime.TryParse(ctProp.GetString(), out var parsedDt))
                            {
                                created = parsedDt.ToUniversalTime();
                            }

                            rawMessages.Add((mid, mtext, fromId, fromName, created));
                        }

                        if (rawMessages.Count == 0) continue;

                        // Időrendi sorrend (legrégebbi elöl)
                        rawMessages.Reverse();

                        // Ellenőrizzük, hogy a szalon korábban törölte-e ezt a beszélgetést (ClearedSenderTimestamps vízjel):
                        // ha igen, csak a törlés UTÁNI vadonatúj üzeneteket engedjük be!
                        inbox.Config.ClearedSenderTimestamps ??= new Dictionary<string, DateTime>();
                        inbox.Config.ProcessedSenderTimestamps ??= new Dictionary<string, DateTime>();

                        DateTime? clearedUntil = null;
                        if (!string.IsNullOrWhiteSpace(clientSenderId) &&
                            inbox.Config.ClearedSenderTimestamps.TryGetValue(clientSenderId.Trim(), out var cById))
                        {
                            clearedUntil = cById;
                        }
                        if (!string.IsNullOrWhiteSpace(clientName) &&
                            inbox.Config.ClearedSenderTimestamps.TryGetValue(clientName.Trim().ToLowerInvariant(), out var cByName))
                        {
                            if (!clearedUntil.HasValue || cByName > clearedUntil.Value)
                                clearedUntil = cByName;
                        }

                        if (clearedUntil.HasValue)
                        {
                            rawMessages = rawMessages
                                .Where(rm => rm.CreatedAt > clearedUntil.Value.AddSeconds(2))
                                .ToList();

                            if (rawMessages.Count == 0)
                            {
                                continue;
                            }
                        }

                        // Ellenőrizzük, hogy ehhez a szalonhoz tartozik-e a beszélgetés
                        if (route == "central_alias")
                        {
                            string? detectedTag = null;
                            foreach (var rm in rawMessages)
                            {
                                var match = Regex.Match(rm.Text, @"#([a-zA-Z0-9_\-]+)");
                                if (match.Success)
                                {
                                    detectedTag = match.Groups[1].Value;
                                    break;
                                }
                            }

                            if (!string.IsNullOrWhiteSpace(detectedTag) && !IsAliasMatchForCompany(dbCompany, inbox, detectedTag))
                            {
                                continue;
                            }
                        }

                        var conv = inbox.Conversations.FirstOrDefault(c =>
                            c.Channel == "Messenger" &&
                            ((!string.IsNullOrWhiteSpace(clientSenderId) && c.ExternalSenderId == clientSenderId) ||
                             string.Equals(c.CustomerName, clientName, StringComparison.OrdinalIgnoreCase)));

                        DateTime? processedUntil = conv?.LastProcessedAt;
                        if (!string.IsNullOrWhiteSpace(clientSenderId) &&
                            inbox.Config.ProcessedSenderTimestamps.TryGetValue(clientSenderId.Trim(), out var pById))
                        {
                            if (!processedUntil.HasValue || pById > processedUntil.Value)
                                processedUntil = pById;
                        }
                        if (!string.IsNullOrWhiteSpace(clientName) &&
                            inbox.Config.ProcessedSenderTimestamps.TryGetValue(clientName.Trim().ToLowerInvariant(), out var pByName))
                        {
                            if (!processedUntil.HasValue || pByName > processedUntil.Value)
                                processedUntil = pByName;
                        }

                        if (conv == null)
                        {
                            conv = new ChatConversationDto
                            {
                                Id = Guid.NewGuid().ToString("N"),
                                Channel = "Messenger",
                                SourceRoute = route,
                                ExternalSenderId = clientSenderId,
                                CustomerName = clientName,
                                UnreadCount = 1,
                                UpdatedAt = rawMessages.Last().CreatedAt,
                                LastProcessedAt = processedUntil
                            };
                            inbox.Conversations.Insert(0, conv);
                            anyModified = true;
                        }
                        else
                        {
                            if (!string.IsNullOrWhiteSpace(clientSenderId) && conv.ExternalSenderId != clientSenderId)
                            {
                                conv.ExternalSenderId = clientSenderId;
                                anyModified = true;
                            }
                            if (!string.IsNullOrWhiteSpace(clientName) && clientName != "Messenger Vendég" && conv.CustomerName != clientName)
                            {
                                conv.CustomerName = clientName;
                                anyModified = true;
                            }
                            if (processedUntil.HasValue && (!conv.LastProcessedAt.HasValue || processedUntil.Value > conv.LastProcessedAt.Value))
                            {
                                conv.LastProcessedAt = processedUntil;
                                anyModified = true;
                            }
                        }

                        foreach (var rm in rawMessages)
                        {
                            bool isSalon = rm.FromId == centralPageId ||
                                           (!string.IsNullOrWhiteSpace(inbox.Config.CustomFacebookPageId) && rm.FromId == inbox.Config.CustomFacebookPageId) ||
                                           string.Equals(rm.FromName, "BeeMagic", StringComparison.OrdinalIgnoreCase);

                            bool isMsgProcessed = isSalon || (processedUntil.HasValue && rm.CreatedAt <= processedUntil.Value.AddSeconds(2));

                            var existingMsg = conv.Messages.FirstOrDefault(em =>
                                (!string.IsNullOrWhiteSpace(rm.Id) && em.Id == rm.Id) ||
                                (string.Equals(em.Text.Trim(), rm.Text.Trim(), StringComparison.OrdinalIgnoreCase) &&
                                 Math.Abs((em.Timestamp - rm.CreatedAt).TotalMinutes) < 10));

                            if (existingMsg == null)
                            {
                                conv.Messages.Add(new ChatMessageItemDto
                                {
                                    Id = !string.IsNullOrWhiteSpace(rm.Id) ? rm.Id : Guid.NewGuid().ToString("N"),
                                    Sender = isSalon ? "salon" : "client",
                                    SenderName = isSalon ? dbCompany.Name : clientName,
                                    Text = rm.Text,
                                    Timestamp = rm.CreatedAt,
                                    SourceRoute = route,
                                    IsProcessed = isMsgProcessed
                                });
                                conv.UpdatedAt = rm.CreatedAt;
                                anyModified = true;
                            }
                            else if (isMsgProcessed && !existingMsg.IsProcessed)
                            {
                                existingMsg.IsProcessed = true;
                                anyModified = true;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Facebook Page Conversations Graph API szinkronizáció sikertelen.");
                }
            }

            return anyModified;
        }

        // Ha localhoston futunk (ahol nincs közvetlen FB webhook / token), lehúzzuk az éles BeeMagic szerverről a szalon üzeneteit
        private async Task<bool> TrySyncFromProductionBridgeAsync(Company dbCompany, ChatInboxResponseDto inbox)
        {
            try
            {
                var host = HttpContext?.Request?.Host.Host ?? "";
                if (host.Contains("railway.app", StringComparison.OrdinalIgnoreCase)) return false;

                var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(5);
                var resp = await client.GetAsync("https://skanisalon-production.up.railway.app/api/ChatInbox/webhook-status");
                if (!resp.IsSuccessStatusCode) return false;

                using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
                if (!doc.RootElement.TryGetProperty("companies", out var compsArr) || compsArr.ValueKind != JsonValueKind.Array)
                    return false;

                inbox.Config.ClearedSenderTimestamps ??= new Dictionary<string, DateTime>();
                inbox.Config.ProcessedSenderTimestamps ??= new Dictionary<string, DateTime>();
                bool anyModified = false;

                foreach (var compEl in compsArr.EnumerateArray())
                {
                    var remoteAlias = compEl.TryGetProperty("alias", out var alProp) ? alProp.GetString() ?? "" : "";
                    var remoteId = compEl.TryGetProperty("id", out var idProp) && idProp.TryGetInt32(out var rid) ? rid : 0;

                    if (remoteId != 7 && !IsAliasMatchForCompany(dbCompany, inbox, remoteAlias))
                        continue;

                    if (!compEl.TryGetProperty("conversations", out var convsArr) || convsArr.ValueKind != JsonValueKind.Array)
                        continue;

                    foreach (var rc in convsArr.EnumerateArray())
                    {
                        var extId = rc.TryGetProperty("externalSenderId", out var exProp) ? exProp.GetString() ?? "" : "";
                        var cName = rc.TryGetProperty("customerName", out var cnProp) ? cnProp.GetString() ?? "Messenger Vendég" : "Messenger Vendég";
                        var ch = rc.TryGetProperty("channel", out var chProp) ? chProp.GetString() ?? "Messenger" : "Messenger";
                        if (string.IsNullOrWhiteSpace(extId) && !string.Equals(ch, "Messenger", StringComparison.OrdinalIgnoreCase))
                            continue;

                        if (!rc.TryGetProperty("messages", out var msgsArr) || msgsArr.ValueKind != JsonValueKind.Array)
                            continue;

                        DateTime? clearedUntil = null;
                        if (!string.IsNullOrWhiteSpace(extId) && inbox.Config.ClearedSenderTimestamps.TryGetValue(extId.Trim(), out var cById))
                            clearedUntil = cById;
                        if (!string.IsNullOrWhiteSpace(cName) && inbox.Config.ClearedSenderTimestamps.TryGetValue(cName.Trim().ToLowerInvariant(), out var cByName))
                        {
                            if (!clearedUntil.HasValue || cByName > clearedUntil.Value) clearedUntil = cByName;
                        }

                        var remoteMsgs = msgsArr.Deserialize<List<ChatMessageItemDto>>(JsonOptions) ?? new List<ChatMessageItemDto>();
                        if (clearedUntil.HasValue)
                        {
                            remoteMsgs = remoteMsgs.Where(m => m.Timestamp > clearedUntil.Value.AddSeconds(2)).ToList();
                        }
                        if (remoteMsgs.Count == 0) continue;

                        var localConv = inbox.Conversations.FirstOrDefault(c =>
                            c.Channel == ch &&
                            ((!string.IsNullOrWhiteSpace(extId) && c.ExternalSenderId == extId) ||
                             string.Equals(c.CustomerName, cName, StringComparison.OrdinalIgnoreCase)));

                        DateTime? processedUntil = localConv?.LastProcessedAt;
                        if (!string.IsNullOrWhiteSpace(extId) && inbox.Config.ProcessedSenderTimestamps.TryGetValue(extId.Trim(), out var pById))
                        {
                            if (!processedUntil.HasValue || pById > processedUntil.Value) processedUntil = pById;
                        }
                        if (!string.IsNullOrWhiteSpace(cName) && inbox.Config.ProcessedSenderTimestamps.TryGetValue(cName.Trim().ToLowerInvariant(), out var pByName))
                        {
                            if (!processedUntil.HasValue || pByName > processedUntil.Value) processedUntil = pByName;
                        }

                        if (localConv == null)
                        {
                            localConv = new ChatConversationDto
                            {
                                Id = Guid.NewGuid().ToString("N"),
                                Channel = ch,
                                SourceRoute = "central_alias",
                                ExternalSenderId = extId,
                                CustomerName = cName,
                                UnreadCount = 1,
                                UpdatedAt = remoteMsgs.Last().Timestamp,
                                LastProcessedAt = processedUntil
                            };
                            inbox.Conversations.Insert(0, localConv);
                            anyModified = true;
                        }

                        foreach (var rm in remoteMsgs)
                        {
                            bool isProc = rm.IsProcessed || rm.Sender == "salon" || (processedUntil.HasValue && rm.Timestamp <= processedUntil.Value.AddSeconds(2));
                            var existingMsg = localConv.Messages.FirstOrDefault(em =>
                                em.Id == rm.Id ||
                                (string.Equals(em.Text.Trim(), rm.Text.Trim(), StringComparison.OrdinalIgnoreCase) &&
                                 Math.Abs((em.Timestamp - rm.Timestamp).TotalMinutes) < 10));

                            if (existingMsg == null)
                            {
                                rm.IsProcessed = isProc;
                                localConv.Messages.Add(rm);
                                localConv.UpdatedAt = rm.Timestamp;
                                anyModified = true;
                            }
                            else if (isProc && !existingMsg.IsProcessed)
                            {
                                existingMsg.IsProcessed = true;
                                anyModified = true;
                            }
                        }
                    }
                }

                return anyModified;
            }
            catch
            {
                return false;
            }
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
