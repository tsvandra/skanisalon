import { createApp } from 'vue';
import { createPinia } from 'pinia';
import App from './App.vue';
import router from './router';
import i18n from './i18n';
import PrimeVue from 'primevue/config';
import Aura from '@primevue/themes/aura';
import 'primeicons/primeicons.css';
import ToastService from 'primevue/toastservice';
import { useCompanyStore } from '@/stores/companyStore'; // Importáljuk a store-t

const app = createApp(App);
const pinia = createPinia();

app.use(pinia);
// FONTOS: A routert és az i18n-t is hozzáadjuk, 
// de még NEM mountoljuk az appot!
app.use(router);
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

// --- APP INITIALIZER LOGIKA ---
const initApp = async () => {
  const companyStore = useCompanyStore();

  // Megpróbáljuk letölteni a konfigurációt
  // Ez blokkolja az oldal megjelenését, amíg meg nem érkeznek a színek/adatok
  // (Így elkerüljük a villogást)
  await companyStore.fetchPublicConfig();

  // Ha végeztünk, mehet az app indítása
  app.mount('#app');
};

// Indítás
initApp();
