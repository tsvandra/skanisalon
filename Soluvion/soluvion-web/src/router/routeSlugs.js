// src/router/routeSlugs.js
// Lokalizált URL-ek (slug-ok) kezelése. A slug-ok a nyelvi fájlok "routes.*" kulcsaiból jönnek,
// így az automatikus UI-fordítás ezeket is lefordítja, és a UiTranslationManager-ben szerkeszthetők.
// FONTOS: ez a fájl nem importálhat routert vagy store-t (körkörös import elkerülése).
import i18n from '@/i18n';
import masterMessages from '@/locales/hu';

export const DEFAULT_LANG = 'hu';

// Nyelvkód az URL első szegmensében (pl. hu, sk, en)
export const LANG_PATTERN = '[a-z]{2,3}';

// Ékezetmentes, kisbetűs, URL-barát szöveg: "Zákazníci" -> "zakaznici"
export const slugify = (value) =>
  String(value ?? '')
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .toLowerCase()
    .trim()
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-+|-+$/g, '');

const masterSlug = (name) => slugify(masterMessages.routes?.[name]);

// A route (név) slug-ja az adott nyelven. Fallback: mester (hu) slug, végül a route neve.
export const slugFor = (name, lang) => {
  const messages = i18n.global.getLocaleMessage(lang);
  return slugify(messages?.routes?.[name]) || masterSlug(name) || name;
};

// Az összes jelenleg ismert slug-változat egy route-hoz (a router path regex-éhez).
export const slugVariants = (name) => {
  const variants = new Set([masterSlug(name), name]);
  i18n.global.availableLocales.forEach((lang) => variants.add(slugFor(name, lang)));
  return [...variants].filter(Boolean);
};

// A legacy (nyelvi előtag nélküli) magyar útvonal, pl. "/ugyfelek"
export const legacyPath = (name) => `/${masterSlug(name) || name}`;

export const currentLang = () => i18n.global.locale.value || DEFAULT_LANG;

// Router location objektum név + nyelv alapján. Használat: router.push(localizedLocation('customers', lang, { query }))
export const localizedLocation = (name, lang = currentLang(), extra = {}) => ({
  name,
  params: name === 'home' ? { lang } : { lang, slug: slugFor(name, lang) },
  ...extra,
});

// String útvonal (hard redirectekhez, pl. window.location.href)
export const localizedPath = (name, lang = currentLang()) =>
  name === 'home' ? `/${lang}` : `/${lang}/${slugFor(name, lang)}`;

// Értesítés, ha új nyelvi üzenetek (és így új slug-ok) kerültek be: a router ilyenkor újraépíti az útvonalait.
const listeners = new Set();
export const onSlugsChanged = (callback) => {
  listeners.add(callback);
  return () => listeners.delete(callback);
};
export const notifySlugsChanged = () => listeners.forEach((cb) => cb());

