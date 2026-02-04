<script setup>
  import { ref, onMounted } from 'vue';
  import Button from 'primevue/button';
  import Card from 'primevue/card';
  import { useRouter } from 'vue-router';
  import api from '@/services/api'

  const router = useRouter();
  const company = ref(null);
  const loading = ref(true);

  // Környezeti változóból vesszük, melyik szalon adata kell (alapból 1)
  const companyId = import.meta.env.VITE_COMPANY_ID || 1;

  // Amikor az oldal betöltődik (onMounted), elindul az adatlekérés
  onMounted(async () => {
    try {
      const response = await api.getCompanyDetails(companyId);
      company.value = response.data;
      console.log("Sikeres adatbetöltés:", company.value); // Segít a hibakeresésben
    } catch (error) {
      console.error("Hiba az adatok betöltésekor:", error);
    } finally {
      loading.value = false;
    }
  });
  //test
  const goToServices = () => {
    router.push('/szolgaltatasok');
  }

  const goToContact = () => {
    router.push('/kapcsolat');
  }
</script>

<template>
  <div class="home-container">

    <div class="hero-section">
      <div class="hero-content">

        <h1 v-if="loading" class="main-title">Betöltés...</h1>
        <h1 v-else-if="company" class="main-title">{{ company.name }}</h1>
        <h1 v-else class="main-title">SZKÁNI SZALON</h1>

        <p class="subtitle">Stílus. Elegancia. Szépség.</p>

        <div class="hero-buttons">
          <Button label="Árlista megtekintése" icon="pi pi-list" class="p-button-raised p-button-warning" @click="goToServices" style="margin-right: 15px; background: var(--primary-color); border:none" />
          <Button label="Időpontfoglalás" icon="pi pi-calendar" class="p-button-outlined p-button-secondary" style="background: white; color: #333" @click="goToContact" />
        </div>
      </div>
    </div>

    <div class="content-section">
      <Card style="width: 100%; margin-bottom: 2rem; box-shadow: 0 4px 10px rgba(0,0,0,0.1);">
        <template #title>
          Üdvözöllek a {{ company.name }}-ban!
        </template>
        <template #content>
          <p class="intro-text">
            Sok szeretettel várok minden kedves régi és új vendéget a Szkáni Szalonban!
            Célom, hogy a nálunk töltött idő ne csak egy szolgáltatás legyen, hanem igazi feltöltődés.
            Professzionális anyagaim és szakértelmem garancia a minőségre.
          </p>
          <p>
            Tekintsd meg folyamatosan bővülő szolgáltatásaimat és galériámat.
          </p>
        </template>
      </Card>

    </div>

  </div>
</template>

<style scoped>
  /* Stílusok csak ehhez az oldalhoz */

  .home-container {
    width: 100%;
  }

  /* HERO SZEKCIÓ BEÁLLÍTÁSAI */
  .hero-section {
    /* Itt állíthatod be a hátteret. Most egy internetes képet használunk. */
    background-image: linear-gradient(rgba(0, 0, 0, 0.5), rgba(0, 0, 0, 0.5)), url('https://images.unsplash.com/photo-1560066984-138dadb4c035?ixlib=rb-1.2.1&auto=format&fit=crop&w=1920&q=80');
    background-size: cover;
    background-position: center;
    height: 400px; /* A fejléc magassága */
    display: flex;
    align-items: center;
    justify-content: center;
    text-align: center;
    color: white;
    border-radius: 0 0 20px 20px; /* Lekerekített alj */
    margin-bottom: 40px;
  }

  h1 {
    font-size: 3rem;
    margin-bottom: 1rem;
    /* Már nem kell fix szín, mert az App.vue globálisan kezeli, de felülírhatjuk */
    color: var(--primary-color);
  }

  .main-title {
    font-size: 3.5rem;
    font-weight: bold;
    margin: 0;
    letter-spacing: 2px;
    text-transform: uppercase;
  }

  .subtitle {
    font-size: 1.5rem;
    margin-top: 10px;
    margin-bottom: 30px;
    color: #fff; /* Fehér szöveg fekete alapon */
    font-style: italic;
  }

  .cta-button {
    display: inline-block;
    /* JAVÍTÁS: Itt volt a zöld, most Arany (primary) */
    background-color: var(--primary-color);
    color: var(--secondary-color); /* A szöveg rajta legyen fekete a kontraszt miatt */
    padding: 15px 30px;
    font-size: 1.2rem;
    border-radius: 5px;
    font-weight: bold;
    transition: transform 0.2s, background-color 0.2s;
  }

    .cta-button:hover {
      transform: scale(1.05);
      /* Hoverre kicsit világosítunk rajta, vagy sötétítünk */
      filter: brightness(1.1);
    }

  .content-section {
    max-width: 900px;
    margin: 0 auto; /* Középre igazítás */
    padding: 20px;
  }

  .intro-text {
    line-height: 1.6;
    font-size: 1.1rem;
    color: #888;
  }

  /* Mobilon kisebb betűk */
  @media (max-width: 768px) {
    .main-title {
      font-size: 2.5rem;
    }

    .hero-section {
      height: 300px;
    }
  }
</style>
