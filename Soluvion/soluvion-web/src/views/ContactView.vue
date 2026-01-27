<script setup>
  import { ref, inject } from 'vue'; // inject kell a cégadatokhoz!
  import Card from 'primevue/card';
  import InputText from 'primevue/inputtext';
  import Textarea from 'primevue/textarea';
  import Button from 'primevue/button';
  import Message from 'primevue/message';

  // 1. BEHÚZZUK A CÉGADATOKAT
  const company = inject('company');

  const name = ref('');
  const email = ref('');
  const messageText = ref('');
  const submitted = ref(false);

  const sendMessage = () => {
    if (name.value && email.value && messageText.value) {
      submitted.value = true;
      name.value = '';
      email.value = '';
      messageText.value = '';
      setTimeout(() => { submitted.value = false; }, 3000);
    } else {
      alert("Kérlek tölts ki minden mezőt!");
    }
  }
</script>

<template>
  <div class="contact-container">
    <div class="header-section">
      <h1>Kapcsolat</h1>
      <p>Kérdésed van? Időpontot foglalnál? Keress fel bizalommal!</p>
    </div>

    <div class="grid-layout">

      <div class="info-column">

        <Card style="margin-bottom: 20px;" class="custom-card">
          <template #title>
            Elérhetőségeim
          </template>
          <template #content>
            <ul class="contact-list">
              <li v-if="company?.city">
                <i class="pi pi-map-marker icon"></i>
                <div>
                  <strong>Cím:</strong><br>
                  {{ company.postalCode }} {{ company.city }}<br>
                  {{ company.streetName }} {{ company.houseNumber }}.
                </div>
              </li>
              <li v-if="company?.phone">
                <i class="pi pi-phone icon"></i>
                <div>
                  <strong>Telefon:</strong><br>
                  <a :href="`tel:${company.phone}`">{{ company.phone }}</a>
                </div>
              </li>
              <li v-if="company?.email">
                <i class="pi pi-envelope icon"></i>
                <div>
                  <strong>Email:</strong><br>
                  <a :href="`mailto:${company.email}`">{{ company.email }}</a>
                </div>
              </li>
            </ul>
          </template>
        </Card>

        <Card class="custom-card">
          <template #title>
            Nyitvatartás
          </template>
          <template #content>
            <div class="opening-info">

              <div style="margin-bottom: 20px;">
                <i class="pi pi-calendar-clock" style="font-size: 2rem; color: var(--primary-color); margin-bottom: 10px;"></i>
                <h3 style="margin: 0; color: #888;">{{ company?.openingHoursTitle || 'Nyitvatartás' }}</h3>
              </div>

              <p v-if="company?.openingHoursDescription" style="line-height: 1.6; color: #888;">
                {{ company.openingHoursDescription }}
              </p>

              <div v-if="company?.openingTimeSlots"
                   style="background: #f9f9f9; color: #555; padding: 15px; border-radius: 8px; border-left: 4px solid var(--primary-color); margin: 15px 0;"
                   v-html="company.openingTimeSlots">
              </div>

              <p v-if="company?.openingExtraInfo" style="font-size: 0.9rem; margin-top: 20px;">
                {{ company.openingExtraInfo }}
              </p>

              <div class="social-buttons" style="margin-top: 15px; display: flex; gap: 10px; flex-wrap: wrap; justify-content: center;">

                <a v-if="company?.facebookUrl" :href="company.facebookUrl" target="_blank" style="text-decoration: none; flex: 1;">
                  <Button label="Facebook" icon="pi pi-facebook" class="p-button-outlined custom-btn-outline" style="width: 100%;" />
                </a>

                <a v-if="company?.instagramUrl" :href="company.instagramUrl" target="_blank" style="text-decoration: none; flex: 1;">
                  <Button label="Instagram" icon="pi pi-instagram" class="p-button-outlined custom-btn-outline" style="width: 100%;" />
                </a>

                <a v-if="company?.tikTokUrl" :href="company.tikTokUrl" target="_blank" style="text-decoration: none; flex: 1;">
                  <Button label="TikTok" icon="pi pi-video" class="p-button-outlined custom-btn-outline" style="width: 100%;" />
                </a>
              </div>
            </div>
          </template>
        </Card>

      </div>

      <div class="form-column">

        <Card style="margin-bottom: 20px;" class="custom-card">
          <template #title>
            Írj bátran!
          </template>
          <template #content>
            <div v-if="submitted" style="margin-bottom: 15px;">
              <Message severity="success" :closable="false">Köszönöm! Üzenetedet megkaptam.</Message>
            </div>
            <div class="form-group">
              <label>Név</label>
              <InputText v-model="name" placeholder="Ide jön a neved" style="width: 100%" class="custom-input" />
            </div>
            <div class="form-group">
              <label>Email cím</label>
              <InputText v-model="email" placeholder="pelda@email.com" style="width: 100%" class="custom-input" />
            </div>
            <div class="form-group">
              <label>Üzenet</label>
              <Textarea v-model="messageText" rows="4" placeholder="Miben segíthetek?" style="width: 100%" class="custom-input" />
            </div>
            <Button label="Üzenet küldése" icon="pi pi-send" @click="sendMessage" class="custom-btn" />
          </template>
        </Card>

        <div class="map-container" v-if="company?.mapEmbedUrl">
          <iframe :src="company.mapEmbedUrl"
                  width="100%"
                  height="300"
                  style="border:0; border-radius: 12px;"
                  allowfullscreen=""
                  loading="lazy">
          </iframe>
        </div>

      </div>
    </div>
  </div>
</template>

<style scoped>
  .contact-container {
    max-width: 1200px;
    margin: 0 auto;
    padding: 20px;
  }

  .header-section {
    text-align: center;
    margin-bottom: 40px;
  }

    .header-section h1 {
      font-size: 2.5rem;
      color: var(--primary-color);
      margin-bottom: 10px;
    }

    .header-section p {
      color: #ccc;
    }

  .opening-info {
    text-align: center;
  }

  .grid-layout {
    display: grid;
    grid-template-columns: 1fr 1.5fr;
    gap: 30px;
  }

  @media (max-width: 768px) {
    .grid-layout {
      grid-template-columns: 1fr;
    }
  }

  .contact-list {
    list-style: none;
    padding: 0;
    margin: 0;
  }

    .contact-list li {
      display: flex;
      align-items: flex-start;
      margin-bottom: 20px;
      font-size: 1.1rem;
    }

    .contact-list .icon {
      font-size: 1.5rem;
      color: var(--primary-color);
      margin-right: 15px;
      margin-top: 3px;
    }

    .contact-list a {
      color: #888;
      text-decoration: none;
      transition: color 0.2s;
    }

      .contact-list a:hover {
        color: var(--primary-color);
      }

  .form-group {
    margin-bottom: 20px;
  }

    .form-group label {
      display: block;
      margin-bottom: 8px;
      font-weight: bold;
      color: #888;
    }

  .map-container {
    box-shadow: 0 4px 10px rgba(0,0,0,0.1);
    border-radius: 12px;
    overflow: hidden;
  }

  /* Gombok felülírása */
  :deep(.custom-btn) {
    background: var(--primary-color) !important;
    border: 1px solid var(--primary-color) !important;
    color: var(--secondary-color) !important;
    font-weight: bold;
  }

  :deep(.custom-btn:hover) {
    background: #b5952f !important;
    border-color: #b5952f !important;
  }

  /* Outlined gomb */
  :deep(.custom-btn-outline) {
    color: var(--primary-color) !important;
    border-color: var(--primary-color) !important;
  }

  :deep(.custom-btn-outline:hover) {
    background: rgba(212, 175, 55, 0.1) !important;
  }

  /* Input mezők fókuszban */
  :deep(.custom-input:focus) {
    border-color: var(--primary-color) !important;
    box-shadow: 0 0 0 1px var(--primary-color) !important;
  }
</style>
