import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import api from '@/services/api';
import i18n from '@/i18n';

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
      const sourceLang = activeCompanyDefaultLang.value;
      let sourceMessages = i18n.global.messages.value[sourceLang];

      if (!sourceMessages) {
        sourceMessages = i18n.global.messages.value['hu'] || i18n.global.messages.value['en'];
      }
      const flattenedUi = flattenObject(sourceMessages || {});

      await api.post('/api/Translation/add-language', {
        companyId,
        targetLanguage: targetLang,
        baseUiTranslations: flattenedUi,
        useAi: useAi
      });

      // Frissítjük a listát, de a polling majd kezeli a státuszt
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
      // Lokális frissítés
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

  // --- JAVÍTOTT loadOverrides (1. Probléma megoldása) ---
  const loadOverrides = async (companyId, langCode) => {
    try {
      // 1. LÉPÉS: BÁZIS MEGTEREMTÉSE
      // Megnézzük, van-e már betöltve valami ehhez a nyelvhez
      const currentMessages = i18n.global.getLocaleMessage(langCode);
      const hasContent = Object.keys(currentMessages).length > 0;

      // Ha üres (pl. 'sk'), akkor másoljuk át a 'hu' (vagy default) tartalmát alapnak!
      if (!hasContent) {
        const baseLang = 'hu'; // Vagy activeCompanyDefaultLang.value
        const baseMessages = i18n.global.getLocaleMessage(baseLang);

        // Fontos: JSON parse/stringify a deep copy miatt, hogy ne referencia legyen
        if (Object.keys(baseMessages).length > 0) {
          i18n.global.setLocaleMessage(langCode, JSON.parse(JSON.stringify(baseMessages)));
          console.log(`Base language '${baseLang}' copied to '${langCode}'`);
        } else {
          console.warn(`Base language '${baseLang}' is empty! UI might show keys.`);
        }
      }

      // 2. LÉPÉS: OVERRIDES BETÖLTÉSE
      const response = await api.get(`/api/Translation/overrides/${companyId}/${langCode}`);
      const overrides = response.data;

      // 3. LÉPÉS: MERGE
      // Egyesével beolvasztjuk a változásokat
      Object.keys(overrides).forEach(key => {
        const nestedObject = unflatten({ [key]: overrides[key] });
        i18n.global.mergeLocaleMessage(langCode, nestedObject);
      });

      console.log(`Overrides loaded for ${langCode}:`, overrides);

    } catch (error) {
      console.warn('Nincs egyedi felülírás vagy hiba történt:', error);
    }
  };

  const setLanguage = async (langCode) => {
    // Először betöltjük az adatokat (fontos a sorrend, hogy mire váltunk, legyen adat)
    if (activeCompanyId.value > 0) {
      await loadOverrides(activeCompanyId.value, langCode);
    }

    i18n.global.locale.value = langCode;
    currentLanguage.value = langCode;
    document.querySelector('html').setAttribute('lang', langCode);
  };

  const deepMerge = (target, source) => {
    for (const key in source) {
      if (source[key] instanceof Object && key in target) {
        Object.assign(source[key], deepMerge(target[key], source[key]));
      }
    }
    Object.assign(target || {}, source);
    return target;
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
