<script setup>
  import { ref } from 'vue';
  import Button from 'primevue/button';
  import api from '@/services/api';

  const props = defineProps({
    companyData: {
      type: Object,
      required: true
    }
  });

  const isUploading = ref(false);
  const logoInputRef = ref(null);
  const heroInputRef = ref(null);
  const footerInputRef = ref(null);

  // A feltöltési logika most már kizárólag ebben a komponensben él!
  const handleUpload = async (event, type) => {
    const file = event.target.files[0];
    if (!file) return;

    isUploading.value = true;
    const formData = new FormData();
    formData.append('file', file);

    let endpoint = '';
    if (type === 'logo') endpoint = '/api/Company/upload/logo';
    else if (type === 'hero') endpoint = '/api/Company/upload/hero';
    else endpoint = '/api/Company/upload/footer';

    try {
      const res = await api.post(endpoint, formData, {
        headers: { 'Content-Type': undefined }
      });

      // A props.companyData reaktív, így a szülőben is frissülni fog!
      if (type === 'logo') props.companyData.logoUrl = res.data.url;
      else if (type === 'hero') props.companyData.heroImageUrl = res.data.url;
      else props.companyData.footerImageUrl = res.data.url;

      // Opcionális: sikeres üzenet (ha van globális toast/értesítő rendszered, ide teheted)
    } catch (err) {
      console.error(err);
      alert("Hiba a feltöltés során.");
    } finally {
      isUploading.value = false;
      event.target.value = ""; // Input törlése, hogy ugyanazt a fájlt újra lehessen választani hiba esetén
    }
  };
</script>

<template>
  <div class="settings-appearance">
    <div class="design-section">
      <h3>Logó beállítások</h3>
      <div class="design-controls">
        <div class="preview-box logo-preview">
          <img v-if="companyData.logoUrl" :src="companyData.logoUrl" :style="{ height: companyData.logoHeight + 'px' }" />
          <span v-else>Nincs logó feltöltve</span>
        </div>
        <div class="control-group">
          <Button label="Logó feltöltése" icon="pi pi-upload" @click="logoInputRef.click()" class="p-button-outlined" :loading="isUploading" />
          <input type="file" ref="logoInputRef" hidden @change="(e) => handleUpload(e, 'logo')" accept="image/*" />
          <div class="slider-container">
            <label>Logó mérete: {{ companyData.logoHeight }}px</label>
            <input type="range" v-model="companyData.logoHeight" min="30" max="150" step="2" class="custom-range" />
          </div>
        </div>
      </div>
    </div>

    <hr class="separator" />

    <div class="design-section">
      <h3>Kezdőoldal Borítókép (Hero)</h3>
      <div class="design-controls">
        <div class="preview-box hero-preview"
             :style="{ backgroundImage: `url(${companyData.heroImageUrl || 'https://via.placeholder.com/400x200?text=Alapertelmezett'})` }">
          <span v-if="!companyData.heroImageUrl" style="color:white; z-index:2; text-shadow: 0 0 5px black;">Nincs egyedi kép</span>
          <div class="overlay"></div>
        </div>
        <div class="control-group">
          <Button label="Borítókép cseréje" icon="pi pi-image" @click="heroInputRef.click()" class="p-button-outlined" :loading="isUploading" />
          <input type="file" ref="heroInputRef" hidden @change="(e) => handleUpload(e, 'hero')" accept="image/*" />
          <small style="display:block; margin-top:5px; color:#888;">Ajánlott méret: 1920x400px</small>
        </div>
      </div>
    </div>

    <hr class="separator" />

    <div class="design-section">
      <h3>Lábléc Háttérkép</h3>
      <div class="design-controls">
        <div class="preview-box footer-preview"
             :style="{ backgroundImage: `url(${companyData.footerImageUrl})` }">
          <span v-if="!companyData.footerImageUrl" style="color:white; z-index:2;">Nincs háttérkép</span>
          <div class="overlay"></div>
        </div>
        <div class="control-group">
          <Button label="Lábléc cseréje" icon="pi pi-image" @click="footerInputRef.click()" class="p-button-outlined" :loading="isUploading" />
          <input type="file" ref="footerInputRef" hidden @change="(e) => handleUpload(e, 'footer')" accept="image/*" />
          <div class="slider-container">
            <label>Lábléc magassága: {{ companyData.footerHeight }}px</label>
            <input type="range" v-model="companyData.footerHeight" min="50" max="600" step="10" class="custom-range" />
          </div>
        </div>
      </div>
    </div>

    <hr class="separator" />

    <div class="design-section">
      <h3>Színek</h3>
      <div class="color-grid">
        <div class="field">
          <label>Elsődleges Szín</label>
          <div class="color-input-wrapper">
            <input type="color" v-model="companyData.primaryColor" />
            <span>{{ companyData.primaryColor }}</span>
          </div>
        </div>
        <div class="field">
          <label>Másodlagos Szín</label>
          <div class="color-input-wrapper">
            <input type="color" v-model="companyData.secondaryColor" />
            <span>{{ companyData.secondaryColor }}</span>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
  h3 {
    margin-top: 1.5rem;
    margin-bottom: 0.5rem;
    color: #555;
  }

  .design-section {
    margin-bottom: 20px;
  }

  .design-controls {
    display: flex;
    gap: 20px;
    align-items: flex-start;
    flex-wrap: wrap;
  }

  .preview-box {
    width: 100%;
    max-width: 300px;
    height: 120px;
    border: 1px dashed #ccc;
    display: flex;
    align-items: center;
    justify-content: center;
    background: #f9f9f9;
    overflow: hidden;
    position: relative;
    border-radius: 4px;
  }

  .logo-preview img {
    height: 50px;
    width: auto;
  }

  .footer-preview {
    background-size: auto 100%;
    background-repeat: repeat-x;
    background-position: center bottom;
  }

  .hero-preview {
    background-size: cover;
    background-position: center;
  }

  .overlay {
    position: absolute;
    inset: 0;
    background: linear-gradient(to top, rgba(0,0,0,0.5), transparent);
  }

  .control-group {
    flex: 1;
    display: flex;
    flex-direction: column;
    gap: 15px;
  }

  .slider-container {
    margin-top: 10px;
  }

    .slider-container label {
      display: block;
      margin-bottom: 5px;
      font-weight: bold;
      color: #666;
    }

  .custom-range {
    width: 100%;
    cursor: pointer;
    accent-color: var(--primary-color);
  }

  .separator {
    border: 0;
    border-top: 1px solid #eee;
    margin: 20px 0;
  }

  .color-grid {
    display: flex;
    gap: 20px;
  }

  .field {
    margin-bottom: 1.5rem;
  }

    .field label {
      display: block;
      margin-bottom: 0.5rem;
      font-weight: bold;
      color: #333;
    }

  .color-input-wrapper {
    display: flex;
    align-items: center;
    gap: 10px;
    border: 1px solid #ddd;
    padding: 5px;
    border-radius: 4px;
    width: fit-content;
  }

  input[type="color"] {
    width: 50px;
    height: 40px;
    border: none;
    cursor: pointer;
    background: none;
  }
</style>
