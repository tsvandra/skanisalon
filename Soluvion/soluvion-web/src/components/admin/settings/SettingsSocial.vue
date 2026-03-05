<script setup>
  import InputText from 'primevue/inputtext';

  defineProps({
    companyData: {
      type: Object,
      required: true
    }
  });
</script>

<template>
  <div class="settings-social">
    <div class="field">
      <label>Facebook Link (Teljes URL)</label>
      <InputText v-model="companyData.facebookUrl" class="w-full" />
    </div>

    <div class="field">
      <label>Instagram Link</label>
      <InputText v-model="companyData.instagramUrl" class="w-full" />
    </div>

    <div class="field">
      <label>TikTok Link</label>
      <InputText v-model="companyData.tikTokUrl" class="w-full" />
    </div>

    <div class="field">
      <label>Google Maps Embed URL (Beágyazási link)</label>
      <InputText v-model="companyData.mapEmbedUrl" class="w-full" />
      <small>Másolja be a Google Maps "Megosztás -> Beágyazás" src linkjét.</small>
    </div>
  </div>
</template>

<style scoped>
  .field {
    margin-bottom: 1.5rem;
  }

    .field label {
      display: block;
      margin-bottom: 0.5rem;
      font-weight: bold;
      color: #333;
    }

  .w-full {
    width: 100%;
  }

  small {
    display: block;
    margin-top: 0.25rem;
    color: #666;
  }
</style>
