<script setup>
  import { ref, onMounted, provide } from 'vue';
  import { RouterView } from 'vue-router';
  import AppHeader from '@/components/AppHeader.vue';
  import api from '@/services/api';
  import { DEFAULT_COMPANY_ID } from '@/config';

  const company = ref(null);
  const isLoading = ref(true);

  const parseJwt = (token) => {
    try {
      return JSON.parse(atob(token.split('.')[1]));
    } catch (e) {
      return null;
    }
  };

  const fetchCompanyData = async () => {
    isLoading.value = true;
    let targetId = DEFAULT_COMPANY_ID; // Alap: 7

    // --- 🛠️ ROBUSTUS DEMO MÓD (Javított verzió) ---

    // http://localhost:5173/#/ - skani
    // http://localhost:5173/#/?id=8 - barber
    // http://localhost:5173/#/?id=9 - pink lady

    // 1. Megnézzük a "sima" URL paramétereket
    let demoId = new URLSearchParams(window.location.search).get('id');

    // 2. HA NINCS, megnézzük a Hash (#) utáni részt is! (Ez hiányzott eddig)
    if (!demoId && window.location.hash.includes('?')) {
      const hashPart = window.location.hash.split('?')[1]; // A kérdőjel utáni rész
      demoId = new URLSearchParams(hashPart).get('id');
    }

    // DEBUG: Ezt látnod kell a konzolban (F12)!
    console.log("🔍 URL Elemzés -> Talált ID:", demoId, "| Eredeti URL:", window.location.href);

    if (demoId) {
      console.log("✅ DEMO MÓD SIKERES! Átállás erre az ID-re:", demoId);
      targetId = Number(demoId);
    } else {
      console.log("⚠️ Nem találtam ID-t az URL-ben, marad a Default:", targetId);
    }

    const token = localStorage.getItem('salon_token');

    if (token) {
      const decoded = parseJwt(token);
      // Ellenőrizzük kis és nagybetűvel is, biztos ami biztos
      const tokenCompanyId = decoded?.CompanyId || decoded?.companyId;

      if (tokenCompanyId) {
        console.log("Admin bejelentkezve, ID:", tokenCompanyId);
        targetId = tokenCompanyId;
      } else {
        localStorage.removeItem('salon_token');
      }
    } else {
      console.log("Látogató mód. Alapértelmezett cég betöltése ID:", targetId);
    }

    try {
      // Itt a 'targetId'-t használjuk, ami biztosan létezik
      const res = await api.get(`/api/Company/${targetId}`);
      const data = res.data;
      company.value = data;

      // CSS Változók beállítása
      document.documentElement.style.setProperty('--primary-color', data.primaryColor || '#d4af37');
      document.documentElement.style.setProperty('--secondary-color', data.secondaryColor || '#1a1a1a');
      document.documentElement.style.setProperty('--font-family', "'Playfair Display', serif");

    } catch (error) {
      console.error("KRITIKUS HIBA: Nem sikerült betölteni a cégadatokat.", error);
      if (token && error.response?.status === 401) {
        localStorage.removeItem('salon_token');
        window.location.reload();
      }
    } finally {
      isLoading.value = false;
    }
  };

  provide('company', company);

  onMounted(() => {
    fetchCompanyData();
  });
</script>

<template>
  <div v-if="!isLoading" class="app-wrapper">
    <header>
      <AppHeader />
    </header>
    <main>
      <RouterView />
    </main>
    <footer class="app-footer">
      <p>&copy; {{ new Date().getFullYear() }} {{ company?.name || 'Szalon' }}. Minden jog fenntartva.</p>
    </footer>
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
  .app-footer {
    text-align: center;
    padding: 2rem;
    background-color: #2c2c2c;
    margin-top: auto;
    color: #ccc;
  }

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
</style>
