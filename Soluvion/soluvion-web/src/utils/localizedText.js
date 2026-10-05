// src/utils/localizedText.js
// Többnyelvű szótár ({ hu: '...', sk: '...' }) feloldása.
// Sorrend: aktuális nyelv -> a cég alapnyelve -> bármelyik kitöltött nyelv -> fallback.
// Nincs benne fix nyelv, így a cég alapnyelve szabadon lehet bármi (pl. sk).
export const pickLocalized = (dict, lang, defaultLang, fallback = '') => {
  if (!dict || typeof dict !== 'object') return fallback;

  const direct = dict[lang] || dict[defaultLang];
  if (direct) return direct;

  const anyValue = Object.values(dict).find((value) => typeof value === 'string' && value.trim() !== '');
  return anyValue || fallback;
};

