import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import api from '@/services/api';
import i18n from '@/i18n';
import masterMessages from '@/locales/hu.json'; // Mester sablon

export const useTranslationStore = defineStore('translation', () => {
  const languages = ref([]);
  const currentLanguage = ref('hu');
  const isLoading = ref(false);
  const activeCompanyId = ref(0);
  const activeCompanyDefaultLang = ref('hu');

  const publishedLanguages = computed(() =>
    languages.value.filter(l => l.status === 'Published' || l.isDefault)
  );

  const pendingReviews = computed(() =>
    languages.value.filter(l => l.status === 'ReviewPending')
  );

  const initCompany = (id, defaultLang) => {
    activeCompanyId.value = id;
    activeCompanyDefaultLang.value = defaultLang || 'hu';
  };

  // Helper: Flatten
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

  // Helper: Deep Merge (Objektumok összefésülése)
  const deepMerge = (target, source) => {
    for (const key in source) {
      if (source[key] instanceof Object && key in target && !(source[key] instanceof Array)) {
        Object.assign(source[key], deepMerge(target[key], source[key]));
      }
    }
    Object.assign(target || {}, source);
    return target;
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
      let sourceMessages = i18n.global.messages.value[sourceLang] || i18n.global.messages.value['hu'];

      const flattenedUi = flattenObject(sourceMessages || {});

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

  // --- JAVÍTOTT loadOverrides (Most már egyesíti a rétegeket) ---
  const loadOverrides = async (companyId, langCode) => {
    try {
      // 1. Réteg: Mester Sablon (HU) - Deep Copy!
      // Ez biztosítja, hogy minden kulcs létezzen.
      const baseStructure = JSON.parse(JSON.stringify(masterMessages));

      // 2. Réteg: Jelenlegi betöltött üzenetek (pl. ha van en.json)
      const currentMessages = i18n.global.getLocaleMessage(langCode);
      if (currentMessages && Object.keys(currentMessages).length > 0) {
        deepMerge(baseStructure, currentMessages);
      }

      // 3. Réteg: Adatbázisból jövő felülírások
      const response = await api.get(`/api/Translation/overrides/${companyId}/${langCode}`);
      const overrides = response.data;

      if (overrides && Object.keys(overrides).length > 0) {
        Object.keys(overrides).forEach(key => {
          const nestedObject = unflatten({ [key]: overrides[key] });
          deepMerge(baseStructure, nestedObject);
        });
        console.log(`Overrides loaded and merged for ${langCode}`);
      }

      // 4. Beállítás
      i18n.global.setLocaleMessage(langCode, baseStructure);

    } catch (error) {
      console.warn('Hiba a felülírások betöltésekor:', error);
    }
  };

  const setLanguage = async (langCode) => {
    if (activeCompanyId.value > 0) {
      await loadOverrides(activeCompanyId.value, langCode);
    }
    // Fallback: Ha nincs cég, de a nyelv üres, töltsük be a mestert
    else {
      const msgs = i18n.global.getLocaleMessage(langCode);
      if (!msgs || Object.keys(msgs).length === 0) {
        i18n.global.setLocaleMessage(langCode, JSON.parse(JSON.stringify(masterMessages)));
      }
    }

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
