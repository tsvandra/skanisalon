import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import api from '@/services/api';
import i18n from '@/i18n';
import masterMessages from '@/locales/hu.json'; // Ez a biztos pont

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

  // Helper: Flatten (Csak küldéshez kell)
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

  // Helper: Unflatten (Klasszikus, biztos módszer)
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
      // Biztonságos fallback
      let sourceMessages = i18n.global.messages.value[sourceLang];
      if (!sourceMessages || Object.keys(sourceMessages).length === 0) {
        sourceMessages = masterMessages;
      }

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

  // --- ROBUSZTUS BETÖLTÉS ---
  const loadOverrides = async (companyId, langCode) => {
    try {
      console.log(`Loading language: ${langCode}...`);

      // 1. LÉPÉS: ALAPOZÁS (Reset)
      // Mindig a tiszta masterMessages-t (HU) állítjuk be alapnak.
      // Ezzel garantáljuk, hogy minden kulcs létezik.
      // Fontos: JSON parse/stringify a deep copy miatt.
      i18n.global.setLocaleMessage(langCode, JSON.parse(JSON.stringify(masterMessages)));

      // 2. LÉPÉS: DB ADATOK LETÖLTÉSE
      const response = await api.get(`/api/Translation/overrides/${companyId}/${langCode}`);
      const overrides = response.data; // { "nav.home": "Home", ... }

      // 3. LÉPÉS: RÁOLVASZTÁS (Merge)
      if (overrides && Object.keys(overrides).length > 0) {
        // Átalakítjuk az overrides-t egyetlen nagy nested objektummá
        // Így csak egyszer kell hívni a mergeLocaleMessage-t (gyorsabb és stabilabb)
        const mergedOverrides = {};

        Object.keys(overrides).forEach(key => {
          // Itt egy kis trükk: unflatten egyesével, majd merge a lokális objektumba
          const nested = unflatten({ [key]: overrides[key] });
          // Egyszerű deep merge a lokális temp objektumba
          // (Itt most egyszerűsítve, feltételezve, hogy a unflatten jól működik)
          // De a biztosabb, ha a vue-i18n-re bízzuk:
          i18n.global.mergeLocaleMessage(langCode, nested);
        });

        console.log(`Success: ${Object.keys(overrides).length} overrides applied to ${langCode}`);
      } else {
        console.log(`No overrides found for ${langCode}, using base template.`);
      }

    } catch (error) {
      console.warn('Hiba a felülírások betöltésekor (marad a magyar alap):', error);
    }
  };

  const setLanguage = async (langCode) => {
    // 1. Ha van cég, betöltjük az adatbázisból
    if (activeCompanyId.value > 0) {
      await loadOverrides(activeCompanyId.value, langCode);
    }
    // 2. Ha nincs cég (pl. vendég), de a nyelv üres, akkor is inicializáljuk a masterből
    else {
      const msgs = i18n.global.getLocaleMessage(langCode);
      if (!msgs || Object.keys(msgs).length === 0) {
        i18n.global.setLocaleMessage(langCode, JSON.parse(JSON.stringify(masterMessages)));
      }
    }

    // 3. Váltás
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
