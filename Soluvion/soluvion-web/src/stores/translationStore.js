import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import api from '@/services/api';
import i18n from '@/i18n';

// --- ÚJ IMPORT: Ez a "DNS-e" az alkalmazásnak. Minden nyelv ebből születik. ---
import masterMessages from '@/locales/hu.json';

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

  const fetchLanguages = async (companyId) => {
    try {
      const response = await api.get(`/api/Translation/languages/${companyId}`);
      languages.value = response.data;
    } catch (error) {
      console.error('Hiba a nyelvek betöltésekor:', error);
    }
  };

  // Helper: Flatten
  const flattenObject = (obj, prefix = '') => {
    return Object.keys(obj).reduce((acc, k) => {
      const pre = prefix.length ? prefix + '.' : '';
      if (typeof obj[k] === 'object' && obj[k] !== null) {
        Object.assign(acc, flattenObject(obj[k], pre + k));
      } else {
        acc[pre + k] = obj[k];
      }
      return acc;
    }, {});
  };

  // Helper: Unflatten
  const unflatten = (data) => {
    if (Object(data) !== data || Array.isArray(data)) return data;
    var result = {}, cur, prop, idx, last, temp;
    for (var p in data) {
      cur = result, prop = "", last = 0;
      do {
        idx = p.indexOf(".", last);
        temp = p.substring(last, idx !== -1 ? idx : undefined);
        cur = cur[temp] || (cur[temp] = {});
        prop = temp;
        last = idx + 1;
      } while (idx >= 0);
      cur[prop] = data[p];
    }
    return result[""] || result;
  };

  const addLanguage = async (companyId, targetLang, useAi = true) => {
    isLoading.value = true;
    try {
      // Mindig a betöltött masterMessages-t használjuk alapnak a küldéshez
      const flattenedUi = flattenObject(masterMessages || {});

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

  // --- A LÉNYEG: A NYELV FELÉPÍTÉSE MEMÓRIÁBAN ---
  const loadOverrides = async (companyId, langCode) => {
    try {
      // 1. Megnézzük, létezik-e már a nyelv a vue-i18n rendszerében
      let currentMessages = i18n.global.getLocaleMessage(langCode);

      // Ha üres vagy hiányos, akkor létrehozzuk a Master (HU) alapján
      if (!currentMessages || Object.keys(currentMessages).length === 0) {
        console.log(`Creating '${langCode}' based on Master Template...`);

        // Deep copy a master JSON-ból
        const template = JSON.parse(JSON.stringify(masterMessages));

        // Beállítjuk az új nyelvet. Mostantól az 'sk' létezik, de minden szöveg magyar benne.
        i18n.global.setLocaleMessage(langCode, template);
      }

      // 2. Letöltjük a DB-ből a fordításokat
      const response = await api.get(`/api/Translation/overrides/${companyId}/${langCode}`);
      const overrides = response.data; // { "nav.home": "Domov", ... }

      // 3. Felülírjuk a sablont a tényleges fordításokkal
      if (overrides && Object.keys(overrides).length > 0) {
        Object.keys(overrides).forEach(key => {
          const nestedObject = unflatten({ [key]: overrides[key] });
          i18n.global.mergeLocaleMessage(langCode, nestedObject);
        });
        console.log(`Overrides loaded and merged for ${langCode}`);
      }

    } catch (error) {
      console.warn('Hiba a felülírások betöltésekor (de a sablonnak köszönhetően nem lesz üres):', error);
    }
  };

  const setLanguage = async (langCode) => {
    // Először betöltjük/felépítjük a nyelvet
    if (activeCompanyId.value > 0) {
      await loadOverrides(activeCompanyId.value, langCode);
    } else {
      // Ha valamiért nincs cég ID (pl. vendég mód hiba), akkor is inicializáljuk a sablonból
      // hogy ne haljon meg az oldal
      const currentMessages = i18n.global.getLocaleMessage(langCode);
      if (!currentMessages || Object.keys(currentMessages).length === 0) {
        i18n.global.setLocaleMessage(langCode, JSON.parse(JSON.stringify(masterMessages)));
      }
    }

    // Majd átváltunk rá
    i18n.global.locale.value = langCode;
    currentLanguage.value = langCode;
    document.querySelector('html').setAttribute('lang', langCode);
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
