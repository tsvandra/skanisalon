import { createI18n } from 'vue-i18n';
import hu from './locales/hu.json';
import en from './locales/en.json';

const i18n = createI18n({
  legacy: false, // Composition API mód
  locale: 'hu', // Alapértelmezett nyelv
  fallbackLocale: 'en',
  globalInjection: true, // Hogy a template-ben használhassuk a $t-t
  messages: {
    hu,
    en
  }
});

export default i18n;
