<script setup>
  import { ref, onMounted, provide, watch, computed } from 'vue';
  import { RouterView, useRoute } from 'vue-router';
  import AppHeader from '@/components/AppHeader.vue';
  import TheFooter from '@/components/TheFooter.vue';
  import { useCompanyStore } from '@/stores/companyStore';
  import { useTranslationStore } from '@/stores/translationStore';
  import Toast from 'primevue/toast';
  import { jwtDecode } from "jwt-decode";

  const companyStore = useCompanyStore();
  const translationStore = useTranslationStore();
  const route = useRoute();

  const isLoggedIn = ref(false);
  const isAppReady = ref(false); // <--- AZ ÚJ KAPCSOLÓ

  // --- AUTH STATUS ELLENŐRZÉSE ---
  const checkAuthStatus = async () => {
    const token = localStorage.getItem('salon_token');
    isLoggedIn.value = !!token;

    if (token) {
      try {
        const decoded = jwtDecode(token);
        const companyId = parseInt(decoded.CompanyId || decoded.companyId || 0);

        if (companyId) {
          // Ha admin vagy, inicializáljuk a szerkesztői környezetet
          const defaultLang = companyStore.company?.defaultLanguage || 'hu';
          translationStore.initCompany(companyId, defaultLang);
          await translationStore.fetchLanguages(companyId);

          const savedLang = localStorage.getItem('user-locale');
          const targetLang = savedLang || defaultLang;
          if (targetLang && targetLang !== translationStore.currentLanguage) {
            console.log(`🌍 Induló nyelv beállítása (Admin): ${targetLang}`);
            await translationStore.setLanguage(targetLang);
          }
        }
      } catch (e) {
        console.error("Token decode hiba:", e);
      }
    }
  };

  watch(() => route.path, () => {
    checkAuthStatus();
  });

  const hasPendingReviews = computed(() => translationStore.pendingReviews.length > 0);

  provide('company', computed(() => companyStore.company));
  provide('isLoggedIn', isLoggedIn);

  // --- A FŐ LOGIKA ---
  onMounted(async () => {
    try {
      // 1. Cégadatok betöltése (Ezalatt még a Loading screen megy)
      if (!companyStore.company) {
        await companyStore.fetchPublicConfig();
      }

      // 2. Auth ellenőrzés
      await checkAuthStatus();

      // 3. NYELV BEÁLLÍTÁSA (Mielőtt kirajzolnánk az oldalt!)
      if (companyStore.company) {
        // Inicializáljuk a store-t
        translationStore.initCompany(companyStore.company.id, companyStore.company.defaultLanguage);

        if (!isLoggedIn.value) {
          // Vendég mód: letöltjük a nyelveket
          await translationStore.fetchLanguages(companyStore.company.id);

          // DÖNTÉS: Milyen nyelven induljunk?
          // 1. Megnézzük, van-e a felhasználónak korábban elmentett nyelve
          const savedLang = localStorage.getItem('user-locale');

          // 2. Ha nincs mentett nyelve, akkor használjuk a cég alapértelmezettjét, vagy végül a magyart
          const targetLang = savedLang || companyStore.company.defaultLanguage || 'hu';

          console.log(`🌍 Induló nyelv beállítása: ${targetLang}`);

          // Várjuk meg, amíg betölti az aktuális nyelv (targetLang) szótárait!
          if (targetLang !== translationStore.currentLanguage) {
            await translationStore.setLanguage(targetLang);
          } else {
            // Ha már ez a beállított, akkor is biztos ami biztos töltsük le az overrides-t
            await translationStore.setLanguage(targetLang);
          }
        }
      }
    } catch (error) {
      console.error("Kritikus hiba az indításnál:", error);
    } finally {
      // 4. CSAK MOST HÚZZUK FEL A FÜGGÖNYT!
      // Akármi történt, most már engedjük látni a felületet
      isAppReady.value = true;
    }
  });
</script>

<template>
  <Toast />

  <div v-if="isAppReady && companyStore.company" class="app-wrapper">

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
      <i class="pi pi-spin pi-spinner" style="font-size: 2rem; margin-bottom: 15px; color: #888;"></i>

      <div v-if="companyStore.error" class="text-red-500 mt-4 text-sm">
        Nem sikerült csatlakozni a szerverhez.
      </div>
    </div>
  </div>
</template>

<style>
  /* Globális stílusok */
  body {
    margin: 0;
    font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Helvetica, Arial, sans-serif;
    background-color: #1a1a1a;
    color: #ffffff;
  }

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
    position: fixed;
    top: 0;
    left: 0;
    width: 100%;
    z-index: 9999;
  }

  main {
    flex: 1;
  }

  /* Utility classes */
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
