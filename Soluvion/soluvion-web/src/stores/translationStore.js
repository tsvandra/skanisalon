import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import api from '@/services/api';
import i18n from '@/i18n';
import masterMessages from '@/locales/hu.json'; // HU je základ

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

  // Helper: Flatten (Pre odoslanie dát na backend)
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

  // Helper: Assign Nested (Bezpečné priradenie do objektu podľa cesty "nav.home")
  const setNestedProperty = (obj, path, value) => {
    const keys = path.split('.');
    let current = obj;
    for (let i = 0; i < keys.length; i++) {
      const key = keys[i];
      if (i === keys.length - 1) {
        current[key] = value;
      } else {
        // Ak kľúč neexistuje alebo nie je objekt, vytvoríme ho
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

  // --- DEFINITÍVNY LOAD & MERGE ---
  const loadOverrides = async (companyId, langCode) => {
    try {
      console.log(`[Store] Loading overrides for: ${langCode}...`);

      // 1. Stiahneme overrides z DB (napr. { "nav.home": "Domovská stránka" })
      const response = await api.get(`/api/Translation/overrides/${companyId}/${langCode}`);
      const overrides = response.data || {};

      console.log(`[Store] Overrides count: ${Object.keys(overrides).length}`);

      // 2. Vytvoríme čistú kópiu MASTER (hu) štruktúry
      // Tým zaručíme, že máme VŠETKY kľúče, aj tie, ktoré nie sú v DB.
      const newMessages = JSON.parse(JSON.stringify(masterMessages));

      // 3. Manuálne "prebijeme" hodnoty v objekte
      // Nepoužívame mergeLocaleMessage, ale staviame si vlastný objekt
      Object.keys(overrides).forEach(key => {
        if (overrides[key]) {
          setNestedProperty(newMessages, key, overrides[key]);
        }
      });

      // 4. Nastavíme hotový objekt do i18n
      // setLocaleMessage kompletne nahradí správy pre daný jazyk
      i18n.global.setLocaleMessage(langCode, newMessages);

      console.log(`[Store] Language ${langCode} set successfully.`);
      // Debug: Skontrolujeme, či tam je hodnota
      console.log(`[Check] nav.home is:`, i18n.global.getLocaleMessage(langCode)?.nav?.home);

    } catch (error) {
      console.warn('Hiba a felülírások betöltésekor:', error);
      // Fallback: aj v prípade chyby nastavíme aspoň master, aby UI nezomrelo
      i18n.global.setLocaleMessage(langCode, JSON.parse(JSON.stringify(masterMessages)));
    }
  };

  const setLanguage = async (langCode) => {
    // 1. Najprv načítame a pripravíme dáta
    if (activeCompanyId.value > 0) {
      await loadOverrides(activeCompanyId.value, langCode);
    } else {
      // Fallback pre hosťa bez ID
      i18n.global.setLocaleMessage(langCode, JSON.parse(JSON.stringify(masterMessages)));
    }

    // 2. Až potom prepneme
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
