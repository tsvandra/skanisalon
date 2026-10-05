import { createApp } from 'vue';
import { createPinia } from 'pinia';
import App from './App.vue';
import router from './router';
import i18n from './i18n';
import PrimeVue from 'primevue/config';
import Aura from '@primevue/themes/aura';
import 'primeicons/primeicons.css';
import '@/assets/main.css';
import ToastService from 'primevue/toastservice';
import { useCompanyStore } from '@/stores/companyStore'; // Importáljuk a store-t
import { useTranslationStore } from '@/stores/translationStore';
import { DEFAULT_LANG, LANG_PATTERN } from '@/router/routeSlugs';

const app = createApp(App);
const pinia = createPinia();

app.use(pinia);
// FONTOS: Az i18n-t most hozzáadjuk, de a routert csak az initApp-ban, a cégadatok és
// az URL-ben szereplő nyelv betöltése után (különben az első navigáció a lokalizált slug-ot nem ismerné fel).
// Az appot sem mountoljuk addig!
app.use(i18n);
app.use(PrimeVue, {
  theme: {
    preset: Aura,
    options: {
      // Ez fontos, hogy a CSS változókat (pl --p-primary-color) 
      // tudjuk felülírni a companyStore-ból
      cssLayer: {
        name: 'primevue',
        order: 'tailwind-base, primevue, tailwind-utilities'
      }
    }
  }
});

app.use(ToastService);

// Az URL első szegmense a nyelvkód (pl. /sk/zakaznici). Ezt még a router indulása előtt betöltjük,
// hogy a lokalizált slug-ok már az első navigációnál felismerhetők legyenek.
const preloadUrlLanguage = async () => {
  const company = useCompanyStore().company;
  if (!company) return;

  const translationStore = useTranslationStore();
  translationStore.initCompany(company.id, company.defaultLanguage);

  const firstSegment = window.location.pathname.split('/')[1] || '';
  const lang = new RegExp(`^${LANG_PATTERN}$`).test(firstSegment)
    ? firstSegment
    : (localStorage.getItem('user-locale') || company.defaultLanguage || DEFAULT_LANG);

  await translationStore.setLanguage(lang);
};

// --- APP INITIALIZER LOGIKA ---
const initApp = async () => {
  const companyStore = useCompanyStore();

  // Megpróbáljuk letölteni a konfigurációt
  // Ez blokkolja az oldal megjelenését, amíg meg nem érkeznek a színek/adatok
  // (Így elkerüljük a villogást)
  await companyStore.fetchPublicConfig();

  try {
    await preloadUrlLanguage();
  } catch (error) {
    console.warn('Az induló nyelv előtöltése nem sikerült:', error);
  }

  // A router telepítése indítja az első navigációt
  app.use(router);

  // Ha végeztünk, mehet az app indítása
  app.mount('#app');
};

// Indítás
initApp();
