<script setup>
  import { ref, onMounted, provide } from 'vue';
  import { RouterView } from 'vue-router';
  import AppHeader from './components/AppHeader.vue';
  import api from '@/services/api';

  // Itt tároljuk a cég adatait
  const company = ref(null);
  const isLoading = ref(true);

  const companyId = import.meta.env.VITE_COMPANY_ID || 1;

  // Lekérjük a cég adatait (Jelenleg fixen ID=1, később dinamikus lesz domain alapján)
  const fetchCompanyData = async () => {
    try {
      // Portszámot ellenőrizd! (pl. 7113)
      const res = await api.getCompanyDetails(companyId);
      const data = response.data;
      company.value = data;

        // --- A VARÁZSLAT: CSS Változók beállítása ---
        // Ez "festi át" az egész weboldalt a cég színeire
        document.documentElement.style.setProperty('--primary-color', data.primaryColor || '#d4af37');
        document.documentElement.style.setProperty('--secondary-color', data.secondaryColor || '#1a1a1a');
        document.documentElement.style.setProperty('--font-family', "'Playfair Display', serif");
      }
    } catch (error) {
      console.error("Hiba a cégadatok betöltésekor:", error);
    } finally {
      isLoading.value = false;
    }
  };

  // "Provide": Ezzel elérhetővé tesszük a cég adatait minden komponens számára (pl. Header, Footer)
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
    Betöltés...
  </div>
</template>

<style>
  /* GLOBÁLIS STÍLUSOK (CSS Változók alapértelmezése) */
  :root {
    --primary-color: #d4af37; /* Arany */
    --secondary-color: #1a1a1a; /* Fekete */
    --font-family: 'Playfair Display', serif;
  }

  body {
    margin: 0;
    font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Helvetica, Arial, sans-serif;
    background-color: var(--secondary-color);
    color: #ffffff;
  }

  h1, h2, h3 {
    font-family: var(--font-family); /* Minden címsor használja a beállított betűtípust */
    color: var(--primary-color); /* A címsorok legyenek Aranyak */
  }

  a {
    text-decoration: none;
    color: var(--primary-color); /* A linkek is Aranyak */
  }

</style>

<style scoped>
  /* App specifikus stílusok */
  .app-footer {
    text-align: center;
    padding: 2rem;
    background-color: #2c2c2c;
    margin-top: auto; /* Hogy mindig alul legyen */
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
    font-size: 1.5rem;
    color: var(--primary-color);
  }
</style>
