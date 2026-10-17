namespace Soluvion.API.DTOs.AppointmentDtos
{
    public class ChatMessageItemDto
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Sender { get; set; } = "client"; // "client" | "salon"
        public string SenderName { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string SourceRoute { get; set; } = "central_alias"; // "central_alias" | "custom_page" | "web" | "booking"
        public bool IsProcessed { get; set; }
    }

    public class ChatConversationDto
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Channel { get; set; } = "Messenger"; // "Messenger" | "Viber" | "WhatsApp" | "Web"
        public string SourceRoute { get; set; } = "central_alias"; // "central_alias" | "custom_page" | "web"
        public string ExternalSenderId { get; set; } = string.Empty;
        public int? CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string? CustomerPhone { get; set; }
        public string? CustomerEmail { get; set; }
        public int? PendingAppointmentId { get; set; }
        public int UnreadCount { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastProcessedAt { get; set; }
        public List<ChatMessageItemDto> Messages { get; set; } = new();
    }

    public class ChatChannelConfigDto
    {
        public string SalonAlias { get; set; } = string.Empty;
        public string CentralMessengerLink { get; set; } = string.Empty;
        public string CentralViberLink { get; set; } = string.Empty;
        public string CentralWhatsAppTag { get; set; } = string.Empty;

        // Saját Facebook Oldal / Viber csatorna (párhuzamosan a központi link mellett!)
        public bool CustomFacebookPageConnected { get; set; }
        public string? CustomFacebookPageName { get; set; }
        public string? CustomFacebookPageId { get; set; }
        public string? CustomFacebookPageToken { get; set; }

        public bool CustomViberBotConnected { get; set; }
        public string? CustomViberBotName { get; set; }
        public string? CustomViberBotToken { get; set; }

        // Ügyfelek fokozatos átnevelése a központi BeeMagic linkre
        public bool IncludeCentralLinkInReplies { get; set; } = true;

        // Amíg nincs jóváhagyva a foglalás, automatikusan frissüljön-e az új üzenetek alapján
        public bool AutoUpdatePendingFromChat { get; set; } = true;

        // Törölt és feldolgozott beszélgetések időbélyeg-határvonalai (hogy a régi FB üzenetek ne töltődjenek vissza újra!)
        public Dictionary<string, DateTime> ClearedSenderTimestamps { get; set; } = new();
        public Dictionary<string, DateTime> ProcessedSenderTimestamps { get; set; } = new();
    }

    public class ChatInboxResponseDto
    {
        public ChatChannelConfigDto Config { get; set; } = new();
        public List<ChatConversationDto> Conversations { get; set; } = new();
    }

    public class ChatReplyRequestDto
    {
        public string Text { get; set; } = string.Empty;
        public bool AppendCentralLink { get; set; }
    }

    public class MarkConversationProcessedRequestDto
    {
        public List<string>? MessageIds { get; set; }
    }

    public class OutboundInviteEmailDto
    {
        public string Email { get; set; } = string.Empty;
        public string? CustomerName { get; set; }
        public string? Subject { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class SimulateIncomingChatDto
    {
        public string Channel { get; set; } = "Messenger"; // "Messenger" | "Viber" | "WhatsApp"
        public string SourceRoute { get; set; } = "central_alias"; // "central_alias" | "custom_page"
        public int? PendingAppointmentId { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerPhone { get; set; }
        public string? MessageText { get; set; }
    }

    public class UpdatePendingFromChatResultDto
    {
        public bool Updated { get; set; }
        public int AppointmentId { get; set; }
        public string Summary { get; set; } = string.Empty;
        public AppointmentResponseDto? Appointment { get; set; }
        public AiChatBookingParseResultDto? Parsed { get; set; }
    }

    public class FacebookUserTokenRequestDto
    {
        public string UserAccessToken { get; set; } = string.Empty;
    }

    public class FacebookPageItemDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string AccessToken { get; set; } = string.Empty;
        public string? PictureUrl { get; set; }
    }

    public class FacebookConnectPageRequestDto
    {
        public string PageId { get; set; } = string.Empty;
        public string PageName { get; set; } = string.Empty;
        public string PageAccessToken { get; set; } = string.Empty;
    }
}
