<script setup>
  import Button from 'primevue/button';
  import Card from 'primevue/card';
  import { useRouter } from 'vue-router';

  const router = useRouter();

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
        <h1 class="main-title">SKANI SALON</h1>
        <p class="subtitle">Stílus. Elegancia. Szépség.</p>

        <div class="hero-buttons">
          <Button label="Árlista megtekintése" icon="pi pi-list" class="p-button-raised p-button-warning" @click="goToServices" style="margin-right: 15px;" />
          <Button label="Időpontfoglalás" icon="pi pi-calendar" class="p-button-outlined p-button-secondary" style="background: white; color: #333" @click="goToContact" />
        </div>
      </div>
    </div>

    <div class="content-section">
      <Card style="width: 100%; margin-bottom: 2rem; box-shadow: 0 4px 10px rgba(0,0,0,0.1);">
        <template #title>
          Üdvözöljük Szalonunkban!
        </template>
        <template #content>
          <p class="intro-text">
            Sok szeretettel várunk minden kedves régi és új vendéget a Skani Salonban!
            Célunk, hogy a nálunk töltött idő ne csak egy szolgáltatás legyen, hanem igazi feltöltődés.
            Professzionális anyagaink és szakértelmünk garancia a minőségre.
          </p>
          <p>
            Tekintse meg folyamatosan bővülő szolgáltatásainkat és galériánkat.
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
    font-style: italic;
  }

  .content-section {
    max-width: 900px;
    margin: 0 auto; /* Középre igazítás */
    padding: 20px;
  }

  .intro-text {
    line-height: 1.6;
    font-size: 1.1rem;
    color: #444;
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
