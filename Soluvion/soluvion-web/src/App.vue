<script setup>
  import { ref, onMounted, provide, computed, watch } from 'vue';
  import { RouterView, useRoute } from 'vue-router';
  import AppHeader from '@/components/AppHeader.vue';
  import TheFooter from '@/components/TheFooter.vue';
  import api from '@/services/api';
  import { DEFAULT_COMPANY_ID } from '@/config';
  import { getCompanyIdFromToken } from '@/utils/jwt';
  import { jwtDecode } from "jwt-decode";

  // ÚJ: Toast és Store importálása
  import Toast from 'primevue/toast';
  import { useTranslationStore } from '@/stores/translationStore';

  const company = ref(null);
  const isLoading = ref(true);
  const isLoggedIn = ref(false);

  // ÚJ: Store és Route használata
  const translationStore = useTranslationStore();
  const route = useRoute();

  // --- NYELVI STÁTUSZ ELLENŐRZÉSE ---
  const checkTranslationStatus = () => {
    const token = localStorage.getItem('salon_token');
    if (token) {
      try {
        // Biztos ami biztos alapon dekódolunk vagy a helperrel szedjük ki
        const decoded = jwtDecode(token);
        const companyId = parseInt(decoded.CompanyId || decoded.companyId || 0);
        if (companyId) translationStore.fetchLanguages(companyId);
      } catch (e) {
        console.error("Token decode error in App check:", e);
      }
    }
  };

  // Figyeljük a navigációt (pl. ha belép a user, frissüljön a sáv)
  watch(() => route.path, () => {
    checkTranslationStatus();
    // isLoggedIn státuszt is frissítjük navigációkor (pl logout után)
    isLoggedIn.value = !!localStorage.getItem('salon_token');
  });

  // Van-e függőben lévő fordítás?
  const hasPendingReviews = computed(() => translationStore.pendingReviews.length > 0);


  // --- CÉGADATOK BETÖLTÉSE ---
  const fetchCompanyData = async () => {
    isLoading.value = true;
    let targetId = DEFAULT_COMPANY_ID;

    // DEMO MÓD LOGIKA
    let demoId = new URLSearchParams(window.location.search).get('id');
    if (!demoId && window.location.hash.includes('?')) {
      const hashPart = window.location.hash.split('?')[1];
      demoId = new URLSearchParams(hashPart).get('id');
    }

    if (demoId) {
      targetId = Number(demoId);
    }

    const tokenCompanyId = getCompanyIdFromToken();

    if (tokenCompanyId) {
      targetId = tokenCompanyId;
    } else {
      if (localStorage.getItem('salon_token')) {
        localStorage.removeItem('salon_token');
      }
    }

    try {
      const res = await api.get(`/api/Company/${targetId}`);
      const data = res.data;
      company.value = data;

      translationStore.initCompany(data.id, data.defaultLanguage);

      // CSS Változók beállítása
      document.documentElement.style.setProperty('--primary-color', data.primaryColor || '#d4af37');
      document.documentElement.style.setProperty('--secondary-color', data.secondaryColor || '#1a1a1a');
      document.documentElement.style.setProperty('--font-family', "'Playfair Display', serif");

    } catch (error) {
      console.error("KRITIKUS HIBA: Nem sikerült betölteni a cégadatokat.", error);
      if (tokenCompanyId && error.response?.status === 401) {
        localStorage.removeItem('salon_token');
        window.location.reload();
      }
    } finally {
      isLoading.value = false;
    }
  };

  onMounted(() => {
    isLoggedIn.value = !!localStorage.getItem('salon_token');
    fetchCompanyData();
    checkTranslationStatus(); // Indításkor is csekkoljuk a státuszt
  });

  // Provide a komponenseknek
  provide('company', company);
  provide('isLoggedIn', isLoggedIn);
</script>

<template>
  <Toast />

  <div v-if="!isLoading" class="app-wrapper">

    <div v-if="hasPendingReviews" class="bg-yellow-100 border-b border-yellow-200 p-3 text-center sticky-banner">
      <span class="text-yellow-800 font-medium flex items-center justify-center gap-2">
        <i class="pi pi-exclamation-triangle"></i>
        Figyelem: {{ translationStore.pendingReviews.length }} új nyelv fordítása elkészült és ellenőrzésre vár!
        <router-link to="/settings" class="underline font-bold hover:text-yellow-900">
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
    <i class="pi pi-spin pi-spinner" style="font-size: 2rem; margin-right: 10px;"></i>
    Betöltés...
  </div>
</template>

<style>
  :root {
    --primary-color: #d4af37;
    --secondary-color: #1a1a1a;
    --font-family: 'Playfair Display', serif;
  }

  body {
    margin: 0;
    font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Helvetica, Arial, sans-serif;
    background-color: var(--secondary-color);
    color: #ffffff;
  }

  h1, h2, h3, a {
    font-family: var(--font-family);
    color: var(--primary-color);
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
    color: #d4af37;
  }

  /* Main content padding */
  main {
    flex: 1;
  }

  /* Tailwind-szerű osztályok (ha nincs Tailwind, ez a fallback) */
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

  .font-medium {
    font-weight: 500;
  }

  .font-bold {
    font-weight: 700;
  }

  .flex {
    display: flex;
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

  .underline {
    text-decoration: underline;
  }

  /* Banner stílus */
  .sticky-banner {
    position: relative;
    z-index: 1001; /* Hogy minden felett legyen */
  }
</style>
