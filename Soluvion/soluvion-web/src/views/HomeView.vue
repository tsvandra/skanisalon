<script setup>
  import { computed } from 'vue';
  import Button from 'primevue/button';
  import Card from 'primevue/card';
  import { useRouter } from 'vue-router';
  import { useCompanyStore } from '@/stores/companyStore'; // Store használata inject helyett

  const router = useRouter();
  const companyStore = useCompanyStore(); // Store példány

  // Computed a cégadatokra a store-ból
  const company = computed(() => companyStore.company);

  // Ha van a DB-ben kép, azt használjuk, ha nincs, akkor a default képet.
  const heroImageUrl = computed(() => {
    return company.value?.heroImageUrl || 'https://images.unsplash.com/photo-1560066984-138dadb4c035?ixlib=rb-1.2.1&auto=format&fit=crop&w=1920&q=80';
  });

  const goToServices = () => { router.push('/szolgaltatasok'); }
  const goToContact = () => { router.push('/kapcsolat'); }
</script>

<template>
  <div class="home-container">

    <div class="hero-section" :style="{ backgroundImage: `linear-gradient(rgba(0, 0, 0, 0.5), rgba(0, 0, 0, 0.5)), url(${heroImageUrl})` }">
      <div class="hero-content">
        <h1 v-if="company">{{ company.name }}</h1>
        <div class="hero-buttons">
          <Button :label="$t('home.viewServices')" icon="pi pi-list" class="p-button-raised p-button-warning action-btn" @click="goToServices" />
          <Button :label="$t('home.bookAppointment')" icon="pi pi-calendar" class="p-button-outlined p-button-secondary action-btn-secondary" @click="goToContact" />
        </div>
      </div>
    </div>

    <div class="content-section">
      <Card style="width: 100%; margin-bottom: 2rem; box-shadow: 0 4px 10px rgba(0,0,0,0.1);">
        <template #title>
          {{ $t('home.welcome', { company: company?.name || 'Szalon' }) }}
        </template>
        <template #content>
          <div class="intro-text">
            <p class="intro-lead"><strong>{{ $t('home.introTitle') }}</strong></p>
            {{ $t('home.introText') }}
          </div>
          <p style="margin-top: 1rem;">
            {{ $t('home.checkGallery') }}
          </p>
        </template>
      </Card>

    </div>

  </div>
</template>

<style scoped>
  .home-container {
    width: 100%;
  }

  .hero-section {
    background-size: cover;
    background-position: center;
    height: 450px; /* Kicsit magasabb */
    display: flex;
    align-items: center;
    justify-content: center;
    text-align: center;
    color: white;
    border-radius: 0 0 20px 20px;
    margin-bottom: 40px;
  }

  h1 {
    font-size: 3.5rem;
    margin-bottom: 1.5rem;
    color: white; /* Hero képen a fehér jobban látszik */
    text-shadow: 0 2px 10px rgba(0,0,0,0.7);
    font-family: var(--font-family);
  }

  .hero-buttons {
    margin-top: 20px;
    display: flex;
    justify-content: center;
    gap: 15px;
  }

  /* Gombok stílusa - A CSS változókat használja */
  .action-btn {
    background-color: var(--p-primary-color) !important;
    border: none !important;
    color: var(--p-primary-contrast-color, #000) !important;
    font-weight: bold;
    padding: 10px 20px;
  }

  .action-btn-secondary {
    background-color: white !important;
    color: #333 !important;
    border: none !important;
    font-weight: bold;
    padding: 10px 20px;
  }

  .content-section {
    max-width: 900px;
    margin: 0 auto;
    padding: 20px;
  }

  .intro-text {
    line-height: 1.8;
    font-size: 1.1rem;
    color: #ccc; /* Sötét témához igazítva */
  }

  .intro-lead {
    color: var(--p-primary-color);
    font-size: 1.2rem;
    margin-bottom: 10px;
  }

  @media (max-width: 768px) {
    .hero-section {
      height: 350px;
    }

    h1 {
      font-size: 2.2rem;
    }

    .hero-buttons {
      flex-direction: column;
    }
  }
</style>
