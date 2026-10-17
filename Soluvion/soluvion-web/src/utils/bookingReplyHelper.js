export const COMMUNICATION_CHANNELS = [
  {
    id: 'WhatsApp',
    label: 'WhatsApp',
    icon: 'pi pi-whatsapp',
    badgeClass: 'bg-emerald-500/15 text-emerald-600 border-emerald-500/30',
    buttonClass: 'bg-emerald-600 hover:bg-emerald-500 text-white'
  },
  {
    id: 'SMS',
    label: 'SMS',
    icon: 'pi pi-mobile',
    badgeClass: 'bg-sky-500/15 text-sky-600 border-sky-500/30',
    buttonClass: 'bg-sky-600 hover:bg-sky-500 text-white'
  },
  {
    id: 'Email',
    label: 'E-mail',
    icon: 'pi pi-envelope',
    badgeClass: 'bg-primary/15 text-primary border-primary/30',
    buttonClass: 'bg-primary hover:brightness-110 text-white'
  },
  {
    id: 'Viber',
    label: 'Viber',
    icon: 'pi pi-comments',
    badgeClass: 'bg-purple-500/15 text-purple-600 border-purple-500/30',
    buttonClass: 'bg-purple-600 hover:bg-purple-500 text-white'
  },
  {
    id: 'Messenger',
    label: 'Messenger',
    icon: 'pi pi-facebook',
    badgeClass: 'bg-blue-500/15 text-blue-600 border-blue-500/30',
    buttonClass: 'bg-blue-600 hover:bg-blue-500 text-white'
  }
];

export function getChannelMeta(channelId) {
  if (!channelId) return COMMUNICATION_CHANNELS[0];
  return (
    COMMUNICATION_CHANNELS.find(
      c => c.id.toLowerCase() === String(channelId).toLowerCase()
    ) || COMMUNICATION_CHANNELS[0]
  );
}

export function cleanPhoneForLink(rawPhone) {
  if (!rawPhone) return '';
  let digits = String(rawPhone).replace(/[^\d+]/g, '');
  if (digits.startsWith('00')) {
    digits = '+' + digits.slice(2);
  } else if (digits.startsWith('09')) {
    // Szlovák mobil formátum (pl. 0905...) -> +421905...
    digits = '+421' + digits.slice(1);
  } else if (digits.startsWith('06')) {
    // Magyar mobil formátum (pl. 0630...) -> +3630...
    digits = '+36' + digits.slice(2);
  }
  return digits;
}

export const DEFAULT_CENTRAL_MESSENGER_LINK = 'https://m.me/1455828144269648?ref=skanisalon';

export function buildBookingReplyText({
  type = 'approve',
  lang = 'hu',
  customerName = '',
  companyName = 'Salon',
  slotText = '',
  servicesText = '',
  totalPrice = '',
  centralMessengerLink = DEFAULT_CENTRAL_MESSENGER_LINK,
  includeMessengerLink = true
}) {
  const l = (lang || 'hu').slice(0, 2).toLowerCase();
  const namePart = customerName ? ` ${customerName}` : '';
  const pricePart = totalPrice ? ` (${totalPrice} EUR)` : '';
  const linkToUse = (centralMessengerLink || DEFAULT_CENTRAL_MESSENGER_LINK).trim();

  if (l === 'sk') {
    const messengerFooterSk = includeMessengerLink && linkToUse
      ? `\n\n💬 Ak nám chcete odpovedať alebo upraviť termín cez Messenger, kliknite sem: ${linkToUse}`
      : '';
    if (type === 'reject') {
      return `Dobrý deň${namePart}! Ďakujeme za Váš záujem o termín v salóne ${companyName} (${slotText} – ${servicesText}). Žiaľ, tento termín už nie je voľný. Prosím, napíšte nám a radi Vám navrhneme náhradný termín!${messengerFooterSk}`;
    }
    return `Dobrý deň${namePart}! Potvrdzujeme Vašu rezerváciu v salóne ${companyName}:\n📅 Termín: ${slotText}\n✂️ Služby: ${servicesText}${pricePart}\nTešíme sa na Vašu návštevu!${messengerFooterSk}`;
  }

  if (l === 'en') {
    const messengerFooterEn = includeMessengerLink && linkToUse
      ? `\n\n💬 To reply or reschedule directly on Messenger, tap here: ${linkToUse}`
      : '';
    if (type === 'reject') {
      return `Hello${namePart}! Thank you for your booking request at ${companyName} (${slotText} – ${servicesText}). Unfortunately, this slot is no longer available. Please reply to this message and we will gladly find an alternative time for you!${messengerFooterEn}`;
    }
    return `Hello${namePart}! Your appointment at ${companyName} is confirmed:\n📅 Date & Time: ${slotText}\n✂️ Services: ${servicesText}${pricePart}\nWe look forward to seeing you!${messengerFooterEn}`;
  }

  // Default: HU
  const messengerFooterHu = includeMessengerLink && linkToUse
    ? `\n\n💬 Ha Messengeren szeretnél válaszolni vagy időpontot módosítani, kattints ide: ${linkToUse}`
    : '';
  if (type === 'reject') {
    return `Szia${namePart}! Köszönjük az időpontkérésedet a(z) ${companyName} szalonba (${slotText} – ${servicesText}). Sajnos ez az időpont közben betelt vagy nem elérhető. Kérlek, írj vissza nekünk, és egyeztetünk egy másik megfelelő időpontot!${messengerFooterHu}`;
  }
  return `Szia${namePart}! Visszaigazoljuk a foglalásodat a(z) ${companyName} szalonban:\n📅 Időpont: ${slotText}\n✂️ Kezelés: ${servicesText}${pricePart}\nVárunk szeretettel!${messengerFooterHu}`;
}

export function buildOutboundInviteText({
  lang = 'hu',
  customerName = '',
  companyName = 'Szkáni Szalon',
  customMessage = '',
  centralMessengerLink = DEFAULT_CENTRAL_MESSENGER_LINK
}) {
  const l = (lang || 'hu').slice(0, 2).toLowerCase();
  const namePart = customerName ? ` ${customerName}` : '';
  const linkToUse = (centralMessengerLink || DEFAULT_CENTRAL_MESSENGER_LINK).trim();
  const trimmedBody = (customMessage || '').trim();

  if (l === 'sk') {
    const body = trimmedBody || `Dobrý deň${namePart}! Kontaktujeme Vás zo salónu ${companyName} ohľadom Vášho termínu.`;
    return `${body}\n\n💬 Pre rýchlu odpoveď cez Messenger kliknite na náš priamy odkaz: ${linkToUse}`;
  }

  if (l === 'en') {
    const body = trimmedBody || `Hello${namePart}! We are reaching out from ${companyName} regarding your appointment.`;
    return `${body}\n\n💬 To reply directly on Messenger, tap our salon link: ${linkToUse}`;
  }

  const body = trimmedBody || `Szia${namePart}! A(z) ${companyName} szalonból keresünk az időpontoddal kapcsolatban.`;
  return `${body}\n\n💬 Ha Messengeren szeretnél válaszolni nekünk, kattints erre a közvetlen linkre: ${linkToUse}`;
}

export async function openChannelSmartReply({
  channel = 'WhatsApp',
  phone = '',
  email = '',
  messengerHandle = '',
  subject = '',
  message = ''
}) {
  const ch = String(channel || 'WhatsApp').toLowerCase();
  const cleanPhone = cleanPhoneForLink(phone);
  const waPhone = cleanPhone.replace(/^\+/, '');
  const encodedMsg = encodeURIComponent(message);

  // Vágólapra másoljuk biztonsági tartalékként (Viber / Messenger esetén különösen hasznos)
  try {
    if (navigator.clipboard && message) {
      await navigator.clipboard.writeText(message);
    }
  } catch {
    // Clipboard nem elérhető
  }

  if (ch === 'whatsapp') {
    const targetUrl = waPhone
      ? `https://wa.me/${waPhone}?text=${encodedMsg}`
      : `https://wa.me/?text=${encodedMsg}`;
    window.open(targetUrl, '_blank', 'noopener,noreferrer');
    return 'whatsapp';
  }

  if (ch === 'sms') {
    window.location.href = `sms:${cleanPhone || ''}?body=${encodedMsg}`;
    return 'sms';
  }

  if (ch === 'viber') {
    if (waPhone) {
      window.location.href = `viber://chat?number=%2B${waPhone}`;
    } else {
      window.location.href = `viber://forward?text=${encodedMsg}`;
    }
    return 'viber';
  }

  if (ch === 'messenger') {
    const handle = (messengerHandle || '').trim();
    if (handle && !handle.includes(' ')) {
      window.open(`https://m.me/${encodeURIComponent(handle)}`, '_blank', 'noopener,noreferrer');
    } else {
      window.open('https://www.messenger.com/', '_blank', 'noopener,noreferrer');
    }
    return 'messenger';
  }

  if (ch === 'email') {
    const encodedSubj = encodeURIComponent(subject || 'Időpont visszaigazolás');
    const mailtoUrl = `mailto:${email || ''}?subject=${encodedSubj}&body=${encodedMsg}`;
    const a = document.createElement('a');
    a.href = mailtoUrl;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    return 'email';
  }

  return 'copied';
}
