// src/stores/translationStore.js
import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import { translationApi } from '@/services/translationApi';
import i18n from '@/i18n';
import huMessages from '@/locales/hu';
import skMessages from '@/locales/sk';
import enMessages from '@/locales/en';
import { notifySlugsChanged } from '@/router/routeSlugs';

const staticBundles = {
  hu: huMessages,
  sk: skMessages,
  en: enMessages,
};

const getBaseMessagesFor = (langCode) => {
  return staticBundles[langCode] || skMessages || huMessages;
};

export const useTranslationStore = defineStore('translation', () => {
  // --- STATE ---
  const languages = ref([]);
  const currentLanguage = ref('sk');
  const isLoading = ref(false);
  const activeCompanyId = ref(0);
  const activeCompanyDefaultLang = ref('sk');

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
    activeCompanyDefaultLang.value = defaultLang || 'sk';
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
      // API hívás kiszervezve
      const response = await translationApi.getLanguages(companyId);
      languages.value = response.data;
    } catch (error) {
      console.error('[TranslationStore] Error fetching languages:', error);
    }
  };

  const addLanguage = async (companyId, targetLang, useAi = true) => {
    isLoading.value = true;
    try {
      const sourceLang = activeCompanyDefaultLang.value || 'sk';
      // Biztonságos fallback: ha nincs meg a forrásnyelv, használjuk a szlovákot
      let sourceMessages = i18n.global.messages.value[sourceLang] || i18n.global.messages.value['sk'] || skMessages;

      const flattenedUi = flattenObject(sourceMessages);

      // API hívás kiszervezve
      await translationApi.addLanguage({
        companyId,
        targetLanguage: targetLang,
        baseUiTranslations: flattenedUi,
        useAi: useAi
      });

      await fetchLanguages(companyId);
    } catch (error) {
      console.error('[TranslationStore] Error adding language:', error);
      throw error;
    } finally {
      isLoading.value = false;
    }
  };

  const publishLanguage = async (companyId, langCode) => {
    try {
      // API hívás kiszervezve
      await translationApi.publishLanguage(companyId, langCode);
      const lang = languages.value.find(l => l.languageCode === langCode);
      if (lang) lang.status = 'Published';
    } catch (error) {
      console.error('[TranslationStore] Error publishing language:', error);
    }
  };

  const toggleLanguageStatus = async (companyId, langCode, isActive) => {
    try {
      await translationApi.toggleLanguageStatus(companyId, langCode, isActive);
      const lang = languages.value.find(l => l.languageCode === langCode);
      if (lang) {
        lang.status = isActive ? 'Published' : 'Inactive';
      }
    } catch (error) {
      console.error('[TranslationStore] Error toggling language status:', error);
      throw error;
    }
  };

  const setDefaultLanguage = async (companyId, langCode) => {
    try {
      await translationApi.setDefaultLanguage(companyId, langCode);
      activeCompanyDefaultLang.value = langCode;
      languages.value.forEach(l => {
        if (l.languageCode === langCode) {
          l.isDefault = true;
          l.status = 'Published';
        } else {
          l.isDefault = false;
        }
      });
    } catch (error) {
      console.error('[TranslationStore] Error setting default language:', error);
      throw error;
    }
  };

  const deleteLanguage = async (companyId, langCode) => {
    try {
      // API hívás kiszervezve
      await translationApi.deleteLanguage(companyId, langCode);
      languages.value = languages.value.filter(l => l.languageCode !== langCode);
    } catch (error) {
      console.error('[TranslationStore] Error deleting language:', error);
      throw error;
    }
  };

  // --- VÉGLEGES LOAD & MERGE ---
  const loadOverrides = async (companyId, langCode) => {
    try {
      console.log(`[TranslationStore] Loading overrides for: ${langCode}...`);

      // 1. Letöltjük az overrides-t az adatbázisból (API hívás kiszervezve)
      const response = await translationApi.getOverrides(companyId, langCode);
      const overrides = response.data || {};

      console.log(`[TranslationStore] Overrides count: ${Object.keys(overrides).length}`);

      // 2. Létrehozunk egy tiszta másolatot a nyelv natív alapcsomagjából (HU, SK, EN)
      const baseForLang = getBaseMessagesFor(langCode);
      const newMessages = JSON.parse(JSON.stringify(baseForLang));

      // 3. Manuálisan felülírjuk az egyedi értékeket az objektumban
      Object.keys(overrides).forEach(key => {
        if (overrides[key]) {
          let safeValue = overrides[key];

          if (typeof safeValue === 'string' && safeValue.includes('@') && !safeValue.includes("{'@'}")) {
            safeValue = safeValue.replace(/@/g, "{'@'}");
          }
          setNestedProperty(newMessages, key, safeValue);
        }
      });

      // 4. Beállítjuk a kész objektumot az i18n-be
      i18n.global.setLocaleMessage(langCode, newMessages);
      notifySlugsChanged();

      console.log(`[TranslationStore] Language ${langCode} loaded successfully.`);

    } catch (error) {
      console.warn('[TranslationStore] Error loading overrides:', error);
      // Fallback: hiba esetén a gyári csomagot állítjuk be
      const baseForLang = getBaseMessagesFor(langCode);
      i18n.global.setLocaleMessage(langCode, JSON.parse(JSON.stringify(baseForLang)));
      notifySlugsChanged();
    }
  };

  const setLanguage = async (langCode) => {
    if (activeCompanyId.value > 0) {
      await loadOverrides(activeCompanyId.value, langCode);
    } else {
      const baseForLang = getBaseMessagesFor(langCode);
      i18n.global.setLocaleMessage(langCode, JSON.parse(JSON.stringify(baseForLang)));
      notifySlugsChanged();
    }

    i18n.global.locale.value = langCode;
    currentLanguage.value = langCode;
    document.querySelector('html').setAttribute('lang', langCode);

    localStorage.setItem('user-locale', langCode);
  };

  const resetOverrides = async (companyId, langCode) => {
    try {
      await translationApi.resetOverrides(companyId, langCode);
      await loadOverrides(companyId, langCode);
    } catch (error) {
      console.error('[TranslationStore] Error resetting overrides:', error);
      throw error;
    }
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
    toggleLanguageStatus,
    setDefaultLanguage,
    deleteLanguage,
    setLanguage,
    loadOverrides,
    resetOverrides
  };
});
