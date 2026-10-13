import { createI18n } from 'vue-i18n';
import hu from './locales/hu';
import sk from './locales/sk';
import en from './locales/en';

const savedLocale = localStorage.getItem('user-locale') || 'sk';

const i18n = createI18n({
  legacy: false, // Composition API mód
  locale: savedLocale, // Alapértelmezett nyelv mentett alapján
  fallbackLocale: 'sk',
  globalInjection: true, // Hogy a template-ben használhassuk a $t-t
  messages: {
    hu,
    sk,
    en
  }
});

export default i18n;
