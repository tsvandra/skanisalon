import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import api from '@/services/api';
import i18n from '@/i18n';

export const useTranslationStore = defineStore('translation', () => {
  // --- STATE ---
  const languages = ref([]); // { languageCode: 'sk', status: 'Published', ... }
  const currentLanguage = ref('hu');
  const isLoading = ref(false);
  const activeCompanyId = ref(0); // Ezt az App.vue állítja majd be

  // --- GETTERS ---

  // Vendégeknek: Csak ami publikus vagy alapértelmezett
  const publishedLanguages = computed(() =>
    languages.value.filter(l => l.status === 'Published' || l.isDefault)
  );

  // Adminnak: Ami ellenőrzésre vár (Bannerhez)
  const pendingReviews = computed(() =>
    languages.value.filter(l => l.status === 'ReviewPending')
  );

  // --- ACTIONS ---

  // 1. Aktív cég beállítása (Ezt hívja meg az App.vue betöltéskor)
  const setCompanyId = (id) => {
    activeCompanyId.value = id;
  };

  // 2. Nyelvek listájának lekérése
  const fetchLanguages = async (companyId) => {
    try {
      const response = await api.get(`/Translation/languages/${companyId}`);
      languages.value = response.data;
    } catch (error) {
      console.error('Hiba a nyelvek betöltésekor:', error);
    }
  };

  // 3. Új nyelv hozzáadása (Triggereli a Backend AI folyamatot)
  const addLanguage = async (companyId, targetLang) => {
    isLoading.value = true;
    try {
      await api.post('/Translation/add-language', {
        companyId,
        targetLanguage: targetLang
      });

      // Optimista frissítés
      const existing = languages.value.find(l => l.languageCode === targetLang);
      if (!existing) {
        languages.value.push({
          languageCode: targetLang,
          status: 'Translating',
          isDefault: false
        });
      }
    } catch (error) {
      console.error('Hiba a nyelv hozzáadásakor:', error);
      throw error;
    } finally {
      isLoading.value = false;
    }
  };

  // 4. Publikálás
  const publishLanguage = async (companyId, langCode) => {
    try {
      await api.post('/Translation/publish', { companyId, languageCode: langCode });

      const lang = languages.value.find(l => l.languageCode === langCode);
      if (lang) lang.status = 'Published';

    } catch (error) {
      console.error('Publikálási hiba:', error);
    }
  };

  // 5. HELPER: Lapos kulcsok (pl. "nav.home") átalakítása objektummá
  // Mert a vue-i18n mergeLocaleMessage objektumot vár (pl. { nav: { home: "..." } })
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

  // 6. Felülírások betöltése DB-ből és MERGE az i18n-be
  const loadOverrides = async (companyId, langCode) => {
    try {
      // Backend: Dictionary<string, string> -> { "nav.services": "Kezelések" }
      const response = await api.get(`/Translation/overrides/${companyId}/${langCode}`);
      const overrides = response.data;

      // Minden kulcsot egyesével behúzunk a vue-i18n memóriájába
      Object.keys(overrides).forEach(key => {
        const nestedObject = unflatten({ [key]: overrides[key] });
        i18n.global.mergeLocaleMessage(langCode, nestedObject);
      });

      console.log(`Overrides loaded for ${langCode}:`, overrides);

    } catch (error) {
      // Nem kritikus hiba, ha nincs override, marad az eredeti JSON
      console.warn('Nincs egyedi felülírás vagy hiba történt:', error);
    }
  };

  // 7. FŐ NYELVVÁLTÓ FÜGGVÉNY
  const setLanguage = async (langCode) => {
// a) Beállítjuk a vue-i18n nyelvét
