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
        const response = await api.get('/api/Company/public-config');

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

      // Konzol log, hogy lásd, lefut-e
      console.log(`🎨 SZÍNEZÉS INDUL: ${primaryHex}`);

      // --- PRIME VUE 4 AURA HACK ---
      // Felülírjuk az összes lehetséges árnyalatot a fő színre, 
      // hogy biztosan látszódjon a változás.
      // (Később majd írhatunk egy okosabb függvényt, ami világosít/sötétít)

      const shades = ['50', '100', '200', '300', '400', '500', '600', '700', '800', '900', '950'];

      shades.forEach(shade => {
        root.style.setProperty(`--p-primary-${shade}`, primaryHex);
      });

      // Alap változók
      root.style.setProperty('--p-primary-color', primaryHex);
      root.style.setProperty('--p-primary-emphasis-color', primaryHex); // Hover effektekhez

      // Secondary
      if (secondaryHex) {
        root.style.setProperty('--salon-secondary', secondaryHex);
      }
    }
  }
});
