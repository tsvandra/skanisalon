<script setup>
  import { ref, onMounted, provide, watch, computed, watchEffect } from 'vue';
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
  const isAppReady = ref(false);

  // --- SaaS DINAMIKUS TÉMA INJEKTÁLÁSA A :ROOT-BA ---
  // Amint a cégadatok megváltoznak, ez automatikusan lefut, és beállítja a globális CSS változókat
  watchEffect(() => {
    const c = companyStore.company;
    if (c) {
      const primary = c.primaryColor || '#14b8a6'; // Türkiz
      const secondary = c.secondaryColor || '#1a1a1a'; // Sötétszürke / Fekete

      const root = document.documentElement;
      root.style.setProperty('--primary-color', primary);
      root.style.setProperty('--secondary-color', secondary);

      // Ideiglenes "trükk": a secondary színt használjuk a háttérnek, amíg nem bővítjük a DB-t!
      // Ha a secondary nagyon sötétszürke (#1a1a1a), a fő háttér legyen egy fokkal sötétebb (#0a0a0a).
      const bg = secondary.toLowerCase() === '#1a1a1a' ? '#0a0a0a' : secondary;
      root.style.setProperty('--background-color', bg);
      root.style.setProperty('--surface-color', secondary);
      root.style.setProperty('--text-color', '#ffffff');
      root.style.setProperty('--text-muted-color', '#9ca3af');

      // A body szintű felülírás, hogy a PrimeVue alapértelmezett fehérje eltűnjön:
      document.body.style.backgroundColor = 'var(--background-color)';
      document.body.style.color = 'var(--text-color)';
    }
  });

  // --- AUTH STATUS ELLENŐRZÉSE ---
  const checkAuthStatus = async () => {
    const token = localStorage.getItem('salon_token');
    isLoggedIn.value = !!token;

    if (token) {
      try {
        const decoded = jwtDecode(token);
        const companyId = parseInt(decoded.CompanyId || decoded.companyId || 0);

        if (companyId) {
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
      if (!companyStore.company) {
        await companyStore.fetchPublicConfig();
      }

      await checkAuthStatus();

      if (companyStore.company) {
        translationStore.initCompany(companyStore.company.id, companyStore.company.defaultLanguage);

        if (!isLoggedIn.value) {
          await translationStore.fetchLanguages(companyStore.company.id);

          const savedLang = localStorage.getItem('user-locale');
          const targetLang = savedLang || companyStore.company.defaultLanguage || 'hu';

          console.log(`🌍 Induló nyelv beállítása: ${targetLang}`);

          if (targetLang !== translationStore.currentLanguage) {
            await translationStore.setLanguage(targetLang);
          } else {
            await translationStore.setLanguage(targetLang);
          }
        }
      }
    } catch (error) {
      console.error("Kritikus hiba az indításnál:", error);
    } finally {
      isAppReady.value = true;
    }
  });
</script>

<template>
  <div class="min-h-screen flex flex-col font-sans">
    <Toast />

    <div v-if="isAppReady && companyStore.company" class="flex-1 flex flex-col">

      <div v-if="isLoggedIn && hasPendingReviews" class="bg-yellow-100 border-b border-yellow-300 p-3 text-center relative z-50">
        <span class="text-yellow-800 font-medium flex items-center justify-center gap-2">
          <i class="pi pi-exclamation-triangle"></i>
          Figyelem: {{ translationStore.pendingReviews.length }} új nyelv fordítása elkészült és ellenőrzésre vár!
          <router-link to="/beallitasok" class="underline font-bold hover:text-yellow-900 transition-colors">
            Ugrás a beállításokhoz
          </router-link>
        </span>
      </div>

      <header>
        <AppHeader />
      </header>

      <main class="flex-1 flex flex-col">
        <RouterView />
      </main>

      <TheFooter />

    </div>

    <div v-else class="fixed inset-0 flex flex-col items-center justify-center bg-background z-[9999]">
      <i class="pi pi-spin pi-spinner text-text-muted text-4xl mb-4"></i>

      <div v-if="companyStore.error" class="text-red-500 mt-4 text-sm font-medium">
        Nem sikerült csatlakozni a szerverhez.
      </div>
    </div>

  </div>
</template>
