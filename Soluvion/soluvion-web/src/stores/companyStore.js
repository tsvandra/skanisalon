// src/stores/companyStore.js
import { defineStore } from 'pinia';
import { companyApi } from '@/services/companyApi';

export const useCompanyStore = defineStore('company', {
  state: () => ({
    company: null,
    loading: false,
    error: null
  }),

  getters: {
    currentCompany: (state) => state.company,
    primaryColor: (state) => state.company?.primaryColor || '#14b8a6', // Alapértelmezett Türkiz
  },

  actions: {
    async fetchPublicConfig() {
      this.loading = true;
      this.error = null;
      try {
        const response = await companyApi.getPublicConfig();
        this.company = response.data;

        // DYNAMIC THEME ALKALMAZÁSA - A teljes objektumot átadjuk
        this.applyTheme(this.company);

        // Tab cím beállítása
        document.title = this.company?.name || 'Skani Salon';

      } catch (err) {
        console.error("Nem sikerült betölteni a cég adatait:", err);
        this.error = err;
      } finally {
        this.loading = false;
      }
    },

    applyTheme(companyData) {
      if (!companyData) return;

      // DEFENSIVE PROGRAMMING: Biztosítjuk, hogy mindig legyen valamilyen string, amivel dolgozhatunk
      const primaryHex = (companyData.primaryColor || '#14b8a6').toString();
      const secondaryHex = (companyData.secondaryColor || '#1a1a1a').toString();

      const root = document.documentElement;

      // 1. PRIME VUE 4 VÁLTOZÓK
      const shades = ['50', '100', '200', '300', '400', '500', '600', '700', '800', '900', '950'];
      shades.forEach(shade => {
        root.style.setProperty(`--p-primary-${shade}`, primaryHex);
      });
      root.style.setProperty('--p-primary-color', primaryHex);
      root.style.setProperty('--p-primary-emphasis-color', primaryHex);

      // 2. TAILWIND & SAAS GLOBÁLIS VÁLTOZÓK
      root.style.setProperty('--primary-color', primaryHex);
      root.style.setProperty('--secondary-color', secondaryHex);

      // Háttér és felület színek dinamikus számolása (biztonságos toLowerCase hívás)
      const bg = secondaryHex.toLowerCase() === '#1a1a1a' ? '#0a0a0a' : secondaryHex;
      root.style.setProperty('--background-color', bg);
      root.style.setProperty('--surface-color', secondaryHex);

      // Szövegszínek és Betűtípus
      root.style.setProperty('--text-color', '#ffffff');
      root.style.setProperty('--text-muted-color', '#9ca3af');
      root.style.setProperty('--font-family', "'Playfair Display', serif");

      // 3. BODY FELÜLÍRÁSA
      document.body.style.backgroundColor = 'var(--background-color)';
      document.body.style.color = 'var(--text-color)';
    }
  }
});
