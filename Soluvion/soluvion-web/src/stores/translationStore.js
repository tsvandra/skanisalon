import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import api from '@/services/api';
import i18n from '@/i18n';

export const useTranslationStore = defineStore('translation', () => {
  // --- STATE ---
  const languages = ref([]);
  const currentLanguage = ref('null');
  const isLoading = ref(false);
  const activeCompanyId = ref(0);
  // ÚJ: Tároljuk a cég alapnyelvét, hogy tudjuk, miről fordítsunk
  const activeCompanyDefaultLang = ref('hu');

  // --- GETTERS ---
  const publishedLanguages = computed(() =>
    languages.value.filter(l => l.status === 'Published' || l.isDefault)
  );

  const pendingReviews = computed(() =>
    languages.value.filter(l => l.status === 'ReviewPending')
  );

  // --- ACTIONS ---

  // 1. JAVÍTOTT: Init függvény, ami a nyelvet is beállítja
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

  // Helper
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

  // Helper unflatten
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

  // 2. JAVÍTOTT: Dinamikus forrásnyelv választás
  const addLanguage = async (companyId, targetLang) => {

    if (!activeCompanyDefaultLang.value) {
      console.warn("Még nem töltődött be a cég alapnyelve, várunk...");
      return;
    }

    isLoading.value = true;
    try {
      // Megkeressük a cég alapnyelvét (pl. 'en')
      const sourceLang = activeCompanyDefaultLang.value;

      // Megpróbáljuk betölteni a hozzá tartozó üzeneteket a statikus JSON-ból
      // Ha nincs meg a rendszerben (pl. egyedi alapnyelv), akkor fallback 'hu' vagy 'en'
      let sourceMessages = i18n.global.messages.value[sourceLang];

      if (!sourceMessages) {
        console.warn(`Nincs statikus JSON fájl a(z) '${sourceLang}' nyelvhez, fallback 'hu' használata.`);
        sourceMessages = i18n.global.messages.value['hu'] || i18n.global.messages.value['en'];
      }

      // Lapítjuk a JSON-t a küldéshez
      const flattenedUi = flattenObject(sourceMessages || {});

      await api.post('/api/Translation/add-language', {
        companyId,
        targetLanguage: targetLang,
        baseUiTranslations: flattenedUi
      });

      const existing = languages.value.find(l => l.languageCode === targetLang);
      if (!existing) {
        languages.value.push({
          languageCode: targetLang,
          status: 'Translating',
          isDefault: false
        });
      } else {
        existing.status = 'Translating';
      }
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

  const loadOverrides = async (companyId, langCode) => {
    try {
      const response = await api.get(`/api/Translation/overrides/${companyId}/${langCode}`);
      const overrides = response.data;

      Object.keys(overrides).forEach(key => {
        const nestedObject = unflatten({ [key]: overrides[key] });
        i18n.global.mergeLocaleMessage(langCode, nestedObject);
      });

      console.log(`Overrides loaded for ${langCode}:`, overrides);

    } catch (error) {
      // Silent fail, ha nincs override
    }
  };

  const setLanguage = async (langCode) => {
    i18n.global.locale.value = langCode;
    currentLanguage.value = langCode;
    document.querySelector('html').setAttribute('lang', langCode);

    if (activeCompanyId.value > 0) {
      await loadOverrides(activeCompanyId.value, langCode);
    }
  };

  return {
    languages,
    currentLanguage,
    isLoading,
    activeCompanyId,
    activeCompanyDefaultLang, // Exportáljuk, ha kellene máshol
    publishedLanguages,
    pendingReviews,
    initCompany, // setCompanyId helyett ezt használjuk
    fetchLanguages,
    addLanguage,
    publishLanguage,
    setLanguage,
    loadOverrides
  };
});
