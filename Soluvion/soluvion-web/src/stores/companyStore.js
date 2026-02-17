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
    primaryColor: (state) => state.company?.primaryColor || '#d4af37',
  },

  actions: {
    async fetchPublicConfig() {
      this.loading = true;
      this.error = null;
      try {
        // Most már az api.js automatikusan kezeli a headereket (URL-ből vagy .env-ből)
        const response = await api.get('/api/Company/public-config');

        this.company = response.data;

        // DYNAMIC THEME ALKALMAZÁSA
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

      // 1. PRIME VUE 4 VÁLTOZÓK (Az új komponensekhez)
      const shades = ['50', '100', '200', '300', '400', '500', '600', '700', '800', '900', '950'];
      shades.forEach(shade => {
        root.style.setProperty(`--p-primary-${shade}`, primaryHex);
      });
      root.style.setProperty('--p-primary-color', primaryHex);
      root.style.setProperty('--p-primary-emphasis-color', primaryHex);

      // 2. LEGACY VÁLTOZÓK (Hogy a régi dizájn is megjavuljon!)
      // Ez volt a hiba oka: a régi gombok ezt keresték, de nem találták.
      root.style.setProperty('--primary-color', primaryHex);

      if (secondaryHex) {
        root.style.setProperty('--salon-secondary', secondaryHex);
        root.style.setProperty('--secondary-color', secondaryHex); // Legacy név
      } else {
        root.style.setProperty('--secondary-color', '#1a1a1a');
      }

      // Font (opcionális, ha dinamikus lenne)
      root.style.setProperty('--font-family', "'Playfair Display', serif");
    }
  }
});
