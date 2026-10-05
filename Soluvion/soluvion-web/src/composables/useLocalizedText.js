// src/composables/useLocalizedText.js
// getLocText(dict): az aktuális nyelven adja vissza a szöveget, a cég alapnyelvére (defaultLanguage) esik vissza.
// A locale.value olvasása miatt a template-ben nyelvváltáskor automatikusan frissül.
import { computed } from 'vue';
import { useI18n } from 'vue-i18n';
import { useCompanyStore } from '@/stores/companyStore';
import { DEFAULT_LANG } from '@/router/routeSlugs';
import { pickLocalized } from '@/utils/localizedText';

export function useLocalizedText() {
  const { locale } = useI18n();
  const companyStore = useCompanyStore();

  const defaultLang = computed(() => companyStore.company?.defaultLanguage || DEFAULT_LANG);

  const getLocText = (dict, fallback = '') => pickLocalized(dict, locale.value, defaultLang.value, fallback);

  return { getLocText, defaultLang };
}

