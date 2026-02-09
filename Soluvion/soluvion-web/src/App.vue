<script setup>
  import { ref, onMounted, provide } from 'vue';
  import { RouterView } from 'vue-router';
  import AppHeader from '@/components/AppHeader.vue';
  import TheFooter from '@/components/TheFooter.vue'; // ÚJ IMPORT
  import api from '@/services/api';
  import { DEFAULT_COMPANY_ID } from '@/config';
  import { getCompanyIdFromToken } from '@/utils/jwt';

  const company = ref(null);
  const isLoading = ref(true);
  const isLoggedIn = ref(false); // Ezt is provide-oljuk, hogy a Footer tudja

  const fetchCompanyData = async () => {
    isLoading.value = true;
    let targetId = DEFAULT_COMPANY_ID; // Alap: 7

    // --- DEMO MÓD LOGIKA (Változatlan) ---
    let demoId = new URLSearchParams(window.location.search).get('id');
    if (!demoId && window.location.hash.includes('?')) {
      const hashPart = window.location.hash.split('?')[1];
      demoId = new URLSearchParams(hashPart).get('id');
    }

    console.log("🔍 URL Elemzés -> Talált ID:", demoId, "| Eredeti URL:", window.location.href);

    if (demoId) {
      console.log("✅ DEMO MÓD SIKERES! Átállás erre az ID-re:", demoId);
      targetId = Number(demoId);
    } else {
      console.log("⚠️ Nem találtam ID-t az URL-ben, marad a Default:", targetId);
    }

    const tokenCompanyId = getCompanyIdFromToken();

    if (tokenCompanyId) {
      console.log("🔑 Admin bejelentkezve, ID:", tokenCompanyId);
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
  });

  // Provide a komponenseknek
  provide('company', company);
  provide('isLoggedIn', isLoggedIn); // Fontos a Footerhez
</script>

<template>
  <div v-if="!isLoading" class="app-wrapper">
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

  /* Main content padding, hogy ne lógjon bele a sticky headerbe/footerbe */
  main {
    flex: 1;
  }
</style>
