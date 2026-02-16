<script setup>
  import { inject, computed } from 'vue';
  import Button from 'primevue/button';
  import Card from 'primevue/card';
  import { useRouter } from 'vue-router';

  const router = useRouter();
  const company = inject('company');

  // Ha van a DB-ben kép, azt használjuk, ha nincs, akkor a default Unsplash képet.
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
        <div class="hero-buttons">
          <Button :label="$t('home.viewServices')" icon="pi pi-list" class="p-button-raised p-button-warning" @click="goToServices" style="margin-right: 15px; background: var(--primary-color); border:none" />
          <Button :label="$t('home.bookAppointment')" icon="pi pi-calendar" class="p-button-outlined p-button-secondary" style="background: white; color: #333" @click="goToContact" />
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
            <p style="color: #ddd"><strong>{{ $t('home.introTitle') }}</strong></p>
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
  /* Stílusok változatlanok */
  .home-container {
    width: 100%;
  }

  .hero-section {
    background-size: cover;
    background-position: center;
    height: 400px;
    display: flex;
    align-items: center;
    justify-content: center;
    text-align: center;
    color: white;
    border-radius: 0 0 20px 20px;
    margin-bottom: 40px;
  }

  h1 {
    font-size: 3rem;
    margin-bottom: 1rem;
    color: var(--primary-color);
  }

  .hero-buttons {
    margin-top: 20px;
  }

  .content-section {
    max-width: 900px;
    margin: 0 auto;
    padding: 20px;
  }

  .intro-text {
    line-height: 1.6;
    font-size: 1.1rem;
    color: #888;
  }

  @media (max-width: 768px) {
    .hero-section {
      height: 300px;
    }
  }
</style>
