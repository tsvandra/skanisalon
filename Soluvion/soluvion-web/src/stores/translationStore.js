import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import api from '@/services/api';
import i18n from '@/i18n';
import masterMessages from '@/locales/hu.json'; // A HU az alap (Master Template)

export const useTranslationStore = defineStore('translation', () => {
  // --- STATE ---
  const languages = ref([]);
  const currentLanguage = ref('hu');
  const isLoading = ref(false);
  const activeCompanyId = ref(0);
  const activeCompanyDefaultLang = ref('hu');

  // --- GETTERS ---
  const publishedLanguages = computed(() =>
    languages.value.filter(l => l.status === 'Published' || l.isDefault)
  );

  const pendingReviews = computed(() =>
    languages.value.filter(l => l.status === 'ReviewPending')
  );

  // --- ACTIONS ---

  const initCompany = (id, defaultLang) => {
    activeCompanyId.value = id;
    activeCompanyDefaultLang.value = defaultLang || 'hu';
  };

  // Helper: Flatten (Adatküldéshez a backendre: {a: {b: 1}} -> {'a.b': 1})
  const flattenObject = (obj, prefix = '') => {
    return Object.keys(obj).reduce((acc, k) => {
      const pre = prefix.length ? prefix + '.' : '';
      if (typeof obj[k] === 'object' && obj[k] !== null && !Array.isArray(obj[k])) {
        Object.assign(acc, flattenObject(obj[k], pre + k));
      } else {
        acc[pre + k] = obj[k];
      }
      return acc;
    }, {});
  };

  // Helper: Assign Nested (Biztonságos hozzárendelés útvonal alapján pl. "nav.home")
  const setNestedProperty = (obj, path, value) => {
    const keys = path.split('.');
    let current = obj;
    for (let i = 0; i < keys.length; i++) {
      const key = keys[i];
      if (i === keys.length - 1) {
        current[key] = value;
      } else {
        // Ha a kulcs nem létezik vagy nem objektum, létrehozzuk
        if (!current[key] || typeof current[key] !== 'object') {
          current[key] = {};
        }
        current = current[key];
      }
    }
  };

  const fetchLanguages = async (companyId) => {
    try {
      const response = await api.get(`/api/Translation/languages/${companyId}`);
      languages.value = response.data;
    } catch (error) {
      console.error('Hiba a nyelvek betöltésekor:', error);
    }
  };

  const addLanguage = async (companyId, targetLang, useAi = true) => {
    isLoading.value = true;
    try {
      const sourceLang = activeCompanyDefaultLang.value;
      // Biztonságos fallback: ha nincs meg a forrásnyelv, használjuk a magyart vagy a masterMessages-t
      let sourceMessages = i18n.global.messages.value[sourceLang] || i18n.global.messages.value['hu'] || masterMessages;

      const flattenedUi = flattenObject(sourceMessages);

      await api.post('/api/Translation/add-language', {
        companyId,
        targetLanguage: targetLang,
        baseUiTranslations: flattenedUi,
        useAi: useAi
      });

      await fetchLanguages(companyId);
    } catch (error) {
      console.error('Hiba a nyelv hozzáadásakor:', error);
      throw error;
    } finally {
      isLoading.value = false;
    }
  };

  const publishLanguage = async (companyId, langCode) => {
    try {
      await api.post('/api/Translation/publish', { companyId, languageCode: langCode });
      const lang = languages.value.find(l => l.languageCode === langCode);
      if (lang) lang.status = 'Published';
    } catch (error) {
      console.error('Publikálási hiba:', error);
    }
  };

  const deleteLanguage = async (companyId, langCode) => {
    try {
      await api.delete(`/api/Translation/language/${companyId}/${langCode}`);
      languages.value = languages.value.filter(l => l.languageCode !== langCode);
    } catch (error) {
      console.error('Hiba a törléskor:', error);
      throw error;
    }
  };

  // --- VÉGLEGES LOAD & MERGE ---
  const loadOverrides = async (companyId, langCode) => {
    try {
      console.log(`[Store] Overrides betöltése ehhez: ${langCode}...`);

      // 1. Letöltjük az overrides-t az adatbázisból (pl. { "nav.home": "Kezdőlap" })
      const response = await api.get(`/api/Translation/overrides/${companyId}/${langCode}`);
      const overrides = response.data || {};

      console.log(`[Store] Talált felülírások száma: ${Object.keys(overrides).length}`);

      // 2. Létrehozunk egy tiszta másolatot a MASTER (hu) struktúrából
      // Ezzel garantáljuk, hogy MINDEN kulcs megvan, az is, ami nincs a DB-ben.
      const newMessages = JSON.parse(JSON.stringify(masterMessages));

      // 3. Manuálisan felülírjuk az értékeket az objektumban
      // Nem használjuk a mergeLocaleMessage-t, hanem saját objektumot építünk, mert ez megbízhatóbb
      Object.keys(overrides).forEach(key => {
        if (overrides[key]) {
          let safeValue = overrides[key];

          if (typeof safeValue === 'string' && safeValue.includes('@') && !safeValue.includes("{'@'}")) {
            safeValue = safeValue.replace(/@/g, "{'@'}");
          }
          setNestedProperty(newMessages, key, overrides[key]);
        }
      });

      // 4. Beállítjuk a kész objektumot az i18n-be
      // a setLocaleMessage teljesen lecseréli az üzeneteket az adott nyelvre
      i18n.global.setLocaleMessage(langCode, newMessages);

      console.log(`[Store] A(z) ${langCode} nyelv sikeresen beállítva.`);
      // Debug: Ellenőrizzük, hogy van-e érték
      console.log(`[Check] nav.home értéke:`, i18n.global.getLocaleMessage(langCode)?.nav?.home);

    } catch (error) {
      console.warn('Hiba a felülírások betöltésekor:', error);
      // Fallback: hiba esetén is beállítjuk a mastert, hogy a UI ne haljon meg (ne legyenek kulcsok)
      i18n.global.setLocaleMessage(langCode, JSON.parse(JSON.stringify(masterMessages)));
    }
  };

  const setLanguage = async (langCode) => {
    // 1. Először betöltjük és előkészítjük az adatokat
    if (activeCompanyId.value > 0) {
      await loadOverrides(activeCompanyId.value, langCode);
    } else {
      // Fallback vendég számára ID nélkül (Master betöltése)
      i18n.global.setLocaleMessage(langCode, JSON.parse(JSON.stringify(masterMessages)));
    }

    // 2. Csak ezután váltunk nyelvet
    i18n.global.locale.value = langCode;
    currentLanguage.value = langCode;
    document.querySelector('html').setAttribute('lang', langCode);

    localStorage.setItem('user-locale', langCode);
  };

  return {
    languages,
    currentLanguage,
    isLoading,
    activeCompanyId,
    activeCompanyDefaultLang,
    publishedLanguages,
    pendingReviews,
    initCompany,
    fetchLanguages,
    addLanguage,
    publishLanguage,
    deleteLanguage,
    setLanguage,
    loadOverrides
  };
});
