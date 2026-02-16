// src/stores/companyStore.js
import { defineStore } from 'pinia';
import api from '@/services/api';

export const useCompanyStore = defineStore('company', {
  state: () => ({
    company: null,
    loading: false,
    error: null
  }),

  getters: {
    currentCompany: (state) => state.company,
    primaryColor: (state) => state.company?.primaryColor || '#10b981', // Default zöld, ha nincs adat
  },

  actions: {
    async fetchPublicConfig() {
      this.loading = true;
      try {
        // A 'api.js' interceptor automatikusan beteszi az X-Tenant-ID-t, 
        // ha van ?forceTenant=7 az URL-ben.
        const response = await api.get('/Company/public-config');

        this.company = response.data;

        // AZONNALI DYNAMIC THEME (Színek beállítása)
        this.applyTheme(this.company.primaryColor, this.company.secondaryColor);

        // Tab cím beállítása
        document.title = this.company.name || 'Skani Salon';

      } catch (err) {
        console.error("Nem sikerült betölteni a cég adatait:", err);
        this.error = err;
      } finally {
        this.loading = false;
      }
    },

    applyTheme(primaryHex, secondaryHex) {
      if (!primaryHex) return;

      const root = document.documentElement;

      // 1. PrimeVue Aura Primary Color felülírása
      // A legegyszerűbb módszer: beállítjuk a fő színt
      // (Profi módban árnyalatokat is kéne generálni, de MVP-nek ez elég)
      root.style.setProperty('--p-primary-color', primaryHex);
      root.style.setProperty('--p-primary-500', primaryHex); // Gombok alap színe

      // Opcionális: Secondary szín (saját használatra)
      if (secondaryHex) {
        root.style.setProperty('--salon-secondary', secondaryHex);
      }
    }
  }
});
