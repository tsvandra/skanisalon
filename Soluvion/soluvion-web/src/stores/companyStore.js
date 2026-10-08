// src/stores/companyStore.js
import { defineStore } from 'pinia';
import { companyApi } from '@/services/companyApi';

// Relatív fénysűrűség (Luminance) számítása (W3C szabvány) a dinamikus SaaS témákhoz
const isDarkColor = (hexColor) => {
  if (!hexColor) return true;
  let hex = hexColor.toString().replace('#', '').trim();
  if (hex.length === 3) {
    hex = hex.split('').map(c => c + c).join('');
  }
  if (hex.length !== 6) return true;
  const r = parseInt(hex.substring(0, 2), 16) / 255;
  const g = parseInt(hex.substring(2, 4), 16) / 255;
  const b = parseInt(hex.substring(4, 6), 16) / 255;

  const toLinear = (c) => (c <= 0.03928 ? c / 12.92 : Math.pow((c + 0.055) / 1.055, 2.4));
  const luminance = 0.2126 * toLinear(r) + 0.7152 * toLinear(g) + 0.0722 * toLinear(b);

  return luminance < 0.45; // 0.45 alatt sötét, felette világos téma
};

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

        // Favicon beállítása
        this.applyFavicon(this.company?.faviconUrl);

      } catch (err) {
        console.error("Nem sikerült betölteni a cég adatait:", err);
        this.error = err;
      } finally {
        this.loading = false;
      }
    },

    applyFavicon(url) {
      if (!url) return;
      let link = document.querySelector("link[rel~='icon']");
      if (!link) {
        link = document.createElement('link');
        link.rel = 'icon';
        document.head.appendChild(link);
      }
      link.removeAttribute('type');
      link.href = url;
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

      // Háttér és felület színek dinamikus számolása
      const bg = secondaryHex.toLowerCase() === '#1a1a1a' ? '#0a0a0a' : secondaryHex;
      root.style.setProperty('--background-color', bg);
      root.style.setProperty('--surface-color', secondaryHex);

      // DINAMIKUS SAAS KONTRASZT: A tenant által választott háttér sötét vagy világos?
      const isDark = isDarkColor(bg);
      const textColor = isDark ? '#ffffff' : '#111827';
      const textMutedColor = isDark ? '#9ca3af' : '#4b5563';
      const colorScheme = isDark ? 'dark' : 'light';

      // Szövegszínek és Betűtípus automatikus beállítása
      root.style.setProperty('--text-color', textColor);
      root.style.setProperty('--text-muted-color', textMutedColor);
      root.style.setProperty('--font-family', "'Playfair Display', serif");

      // 3. BODY FELÜLÍRÁSA
      document.body.style.backgroundColor = 'var(--background-color)';
      document.body.style.color = 'var(--text-color)';

      // 4. BÖNGÉSZŐ FORM ÉS MOBIL TÉMA (dinamikusan 'dark' vagy 'light' a háttér alapján)
      root.style.colorScheme = colorScheme;
      root.style.setProperty('color-scheme', colorScheme);
    }
  }
});

