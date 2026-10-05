<script setup>
  import { ref, onMounted, provide, watch, computed, watchEffect } from 'vue';
  import { RouterView, useRoute, useRouter } from 'vue-router';
  import AppHeader from '@/components/AppHeader.vue';
  import AdminNav from '@/components/admin/AdminNav.vue';
  import TheFooter from '@/components/TheFooter.vue';
  import { useCompanyStore } from '@/stores/companyStore';
  import { useTranslationStore } from '@/stores/translationStore';
  import { useLocalizedRoute } from '@/composables/useLocalizedRoute';
  import Toast from 'primevue/toast';
  import { jwtDecode } from "jwt-decode";

  const companyStore = useCompanyStore();
  const translationStore = useTranslationStore();
  const route = useRoute();
  const router = useRouter();
  const { to: localized } = useLocalizedRoute();

  const isLoggedIn = ref(false);
  const userRole = ref(null);
  const isAppReady = ref(false);

  // --- SaaS DINAMIKUS TÉMA INJEKTÁLÁSA A :ROOT-BA ---
  // Amint a cégadatok megváltoznak, ez automatikusan lefut, és beállítja a globális CSS változókat
  watchEffect(() => {
    const c = companyStore.company;
    if (c) {
      companyStore.applyTheme(c);
    }
  });

  // --- AUTH STATUS ELLENŐRZÉSE ---
  const checkAuthStatus = async () => {
    const token = localStorage.getItem('salon_token');
    isLoggedIn.value = !!token;
    if (!token) userRole.value = null;

    if (token) {
      try {
        const decoded = jwtDecode(token);
        const companyId = parseInt(decoded.CompanyId || decoded.companyId || 0);
        const roleClaim = decoded['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] || decoded.role || decoded.Role;
        userRole.value = roleClaim || 'Customer';

        if (companyId) {
          const defaultLang = companyStore.company?.defaultLanguage || 'hu';
          translationStore.initCompany(companyId, defaultLang);
          await translationStore.fetchLanguages(companyId);

          const savedLang = localStorage.getItem('user-locale');
          const targetLang = route.params.lang || savedLang || defaultLang;
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
  const isEmployee = computed(() => ['Admin', 'Owner', 'Employee'].includes(userRole.value));
  const isAdminRoute = computed(() => {
    if (!isLoggedIn.value || !isEmployee.value) return false;
    const adminPages = ['dashboard', 'orders', 'inventory', 'customers', 'settings'];
    return adminPages.includes(route.name);
  });

  provide('company', computed(() => companyStore.company));
  provide('isLoggedIn', isLoggedIn);
  provide('userRole', userRole);

  // --- A FŐ LOGIKA ---
  onMounted(async () => {
    try {
      // Az URL nyelvkódja csak az első navigáció után érhető el
      await router.isReady();

      if (!companyStore.company) {
        await companyStore.fetchPublicConfig();
      }

      await checkAuthStatus();

      if (companyStore.company) {
        translationStore.initCompany(companyStore.company.id, companyStore.company.defaultLanguage);

        if (!isLoggedIn.value) {
          await translationStore.fetchLanguages(companyStore.company.id);

          const savedLang = localStorage.getItem('user-locale');
          const targetLang = route.params.lang || savedLang || companyStore.company.defaultLanguage || 'hu';

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
          {{ $t('appShell.pendingReviews', { count: translationStore.pendingReviews.length }) }}
          <router-link :to="localized('settings')" class="underline font-bold hover:text-yellow-900 transition-colors">
            {{ $t('appShell.goToSettings') }}
          </router-link>
        </span>
      </div>

      <header>
        <AppHeader />
      </header>

      <AdminNav v-if="isAdminRoute" />

      <main class="flex-1 flex flex-col">
        <RouterView />
      </main>

      <TheFooter />

    </div>

    <div v-else class="fixed inset-0 flex flex-col items-center justify-center bg-background z-[9999]">
      <i class="pi pi-spin pi-spinner text-text-muted text-4xl mb-4"></i>

      <div v-if="companyStore.error" class="text-red-500 mt-4 text-sm font-medium">
        {{ $t('appShell.serverConnectionError') }}
      </div>
    </div>

  </div>
</template>
