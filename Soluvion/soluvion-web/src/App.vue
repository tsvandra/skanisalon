<script setup>
  import { ref, onMounted, provide, watch, computed } from 'vue'; // computed hozzáadva
  import { RouterView, useRoute } from 'vue-router';
  import AppHeader from '@/components/AppHeader.vue';
  import TheFooter from '@/components/TheFooter.vue';

  // STORE-ok importálása (A régi direkt API hívás helyett)
  import { useCompanyStore } from '@/stores/companyStore';
  import { useTranslationStore } from '@/stores/translationStore';

  import Toast from 'primevue/toast';
  import { jwtDecode } from "jwt-decode";

  // Store példányosítása
  const companyStore = useCompanyStore();
  const translationStore = useTranslationStore();
  const route = useRoute();

  const isLoggedIn = ref(false);

  // --- AUTH STATUS ELLENŐRZÉSE (Admin funkciókhoz) ---
  const checkAuthStatus = () => {
    const token = localStorage.getItem('salon_token');
    isLoggedIn.value = !!token;

    if (token) {
      try {
        const decoded = jwtDecode(token);
        const companyId = parseInt(decoded.CompanyId || decoded.companyId || 0);

        // Ha be vagyunk lépve, betöltjük a szerkesztéshez szükséges nyelvi státuszokat is
        if (companyId) {
          translationStore.fetchLanguages(companyId);
        }
      } catch (e) {
        console.error("Token decode hiba:", e);
      }
    }
  };

  // Figyeljük a navigációt
  watch(() => route.path, () => {
    checkAuthStatus();
  });

  // Van-e függőben lévő fordítás? (Admin bannerhez)
  const hasPendingReviews = computed(() => translationStore.pendingReviews.length > 0);

  // --- PROVIDE ---
  // A gyermek komponensek (pl AppHeader) még mindig várhatják a 'company' és 'isLoggedIn' változókat.
  // Átadjuk nekik a Store-ból származó adatot.
  provide('company', computed(() => companyStore.company)); // Reaktív computed property-ként adjuk át
  provide('isLoggedIn', isLoggedIn);

  onMounted(async () => {
    // 1. Auth ellenőrzés
    checkAuthStatus();

    // 2. Cégadatok betöltése
    // A main.js 'initApp'-ja elvileg már elindította, de biztos ami biztos:
    if (!companyStore.company) {
      await companyStore.fetchPublicConfig();
    }
  });
</script>

<template>
  <Toast />

  <div v-if="!companyStore.loading && companyStore.company" class="app-wrapper">

    <div v-if="isLoggedIn && hasPendingReviews" class="bg-yellow-100 border-b border-yellow-200 p-3 text-center sticky-banner">
      <span class="text-yellow-800 font-medium flex items-center justify-center gap-2">
        <i class="pi pi-exclamation-triangle"></i>
        Figyelem: {{ translationStore.pendingReviews.length }} új nyelv fordítása elkészült és ellenőrzésre vár!
        <router-link to="/beallitasok" class="underline font-bold hover:text-yellow-900">
          Ugrás a beállításokhoz
        </router-link>
      </span>
    </div>

    <header>
      <AppHeader />
    </header>

    <main>
      <RouterView />
    </main>

    <TheFooter />

  </div>

  <div v-else class="loading-screen">
    <div class="flex flex-col items-center">
      <i class="pi pi-spin pi-spinner" style="font-size: 2rem; margin-bottom: 15px; color: var(--p-primary-color);"></i>
      <div style="color: white;">Betöltés...</div>
      <div v-if="companyStore.error" class="text-red-500 mt-2 text-sm">
        Hiba történt a kapcsolódáskor.
      </div>
    </div>
  </div>
</template>

<style>
  /* A globális változókat most már a companyStore.js állítja be dinamikusan (applyTheme),
     így innen kivehetjük a fix értékeket, vagy hagyhatjuk fallback-nek. */

  body {
    margin: 0;
    font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Helvetica, Arial, sans-serif;
    background-color: #1a1a1a; /* Fix sötét háttér */
    color: #ffffff;
  }

  /* Linkek színe dinamikus lesz a CSS változók miatt */
  h1, h2, h3, a {
    color: var(--p-primary-color);
  }
</style>

<style scoped>
  .app-wrapper {
    display: flex;
    flex-direction: column;
    min-height: 100vh;
  }

  .loading-screen {
    display: flex;
    justify-content: center;
    align-items: center;
    height: 100vh;
    background-color: #1a1a1a;
  }

  main {
    flex: 1;
  }

  /* Utility classes (Tailwind replacement) */
  .bg-yellow-100 {
    background-color: #fef9c3;
  }

  .border-b {
    border-bottom-width: 1px;
  }

  .border-yellow-200 {
    border-color: #fde047;
  }

  .p-3 {
    padding: 0.75rem;
  }

  .text-center {
    text-align: center;
  }

  .text-yellow-800 {
    color: #854d0e;
  }

  .text-red-500 {
    color: #ef4444;
  }

  .font-medium {
    font-weight: 500;
  }

  .font-bold {
    font-weight: 700;
  }

  .flex {
    display: flex;
  }

  .flex-col {
    flex-direction: column;
  }

  .items-center {
    align-items: center;
  }

  .justify-center {
    justify-content: center;
  }

  .gap-2 {
    gap: 0.5rem;
  }

  .mt-2 {
    margin-top: 0.5rem;
  }

  .text-sm {
    font-size: 0.875rem;
  }

  .underline {
    text-decoration: underline;
  }

  .sticky-banner {
    position: relative;
    z-index: 1001;
  }
</style>
