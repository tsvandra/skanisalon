<script setup>
  import { ref, onMounted } from 'vue';
  // PrimeVue komponensek
  import InputText from 'primevue/inputtext';
  import Textarea from 'primevue/textarea';
  import Button from 'primevue/button';
  import TabView from 'primevue/tabview';
  import TabPanel from 'primevue/tabpanel';
  import api from '@/services/api';
  import { getCompanyIdFromToken } from '@/utils/jwt';

  // ÚJ IMPORTOK
  import LanguageManager from '@/components/admin/LanguageManager.vue';
  import UiTranslationManager from '@/components/admin/UiTranslationManager.vue';

  const companyData = ref({});
  const isLoading = ref(false);
  const isSaving = ref(false);
  const isUploading = ref(false);
  const successMsg = ref('');
  const errorMsg = ref('');

  // Refek a rejtett fájl inputokhoz
  const logoInputRef = ref(null);
  const heroInputRef = ref(null);
  const footerInputRef = ref(null);

  // 2. Adatok betöltése
  const loadCompanyData = async () => {
    const companyId = getCompanyIdFromToken();
    if (!companyId) {
      errorMsg.value = "Nem található cégazonosító. Kérjük, jelentkezz be újra!";
      return;
    }

    isLoading.value = true;
    try {
      const res = await api.get(`/api/Company/${companyId}`);
      const data = res.data;
      if (!data.logoHeight) data.logoHeight = 50;
      if (!data.footerHeight) data.footerHeight = 250;

      companyData.value = { ...data };
    } catch (err) {
      console.error("Hiba a betöltéskor:", err);
      errorMsg.value = "Nem sikerült betölteni a cég adatait.";
    } finally {
      isLoading.value = false;
    }
  };

  // --- FÁJLFELTÖLTÉS LOGIKA ---
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

      if (type === 'logo') companyData.value.logoUrl = res.data.url;
      else if (type === 'hero') companyData.value.heroImageUrl = res.data.url;
      else companyData.value.footerImageUrl = res.data.url;

      successMsg.value = "Kép sikeresen feltöltve!";
      setTimeout(() => successMsg.value = '', 3000);
    } catch (err) {
      console.error(err);
      errorMsg.value = "Hiba a feltöltés során.";
    } finally {
      isUploading.value = false;
    }
  };

  // 3. Mentés
  const saveSettings = async () => {
    const companyId = getCompanyIdFromToken();
    if (!companyId) return;

    isSaving.value = true;
    successMsg.value = '';
    errorMsg.value = '';

    try {
      await api.put(`/api/Company/${companyId}`, companyData.value);

      successMsg.value = "A változtatások sikeresen mentve!";
      if (companyData.value.primaryColor) {
        document.documentElement.style.setProperty('--primary-color', companyData.value.primaryColor);
        document.documentElement.style.setProperty('--secondary-color', companyData.value.secondaryColor);
      }
      setTimeout(() => successMsg.value = '', 3000);

    } catch (err) {
      console.error("Mentési hiba:", err);
      errorMsg.value = err.response?.data || "Hiba történt a mentés során.";
    } finally {
      isSaving.value = false;
    }
  };

  onMounted(() => {
    loadCompanyData();
  });
</script>

<template>
  <div class="settings-container">
    <h1>Cégbeállítások</h1>
    <p class="intro">Itt módosíthatja a weboldalán megjelenő adatokat és a dizájnt.</p>

    <div v-if="successMsg" class="alert-box success">{{ successMsg }}</div>
    <div v-if="errorMsg" class="alert-box error">{{ errorMsg }}</div>
    <div v-if="isLoading" class="alert-box loading">Adatok betöltése...</div>

    <div v-if="!isLoading" class="form-wrapper">
      <TabView>

        <TabPanel header="Elérhetőségek">
          <div class="form-grid">
            <div class="field">
              <label>Cégnév (Weboldalon megjelenő)</label>
              <InputText v-model="companyData.name" class="w-full" />
            </div>
            <div class="field">
              <label>Email</label>
              <InputText v-model="companyData.email" class="w-full" />
            </div>
            <div class="field">
              <label>Telefonszám</label>
              <InputText v-model="companyData.phone" class="w-full" />
            </div>

            <h3>Cím</h3>
            <div class="field-group">
              <div class="field">
                <label>Irányítószám</label>
                <InputText v-model="companyData.postalCode" class="w-full" />
              </div>
              <div class="field">
                <label>Város</label>
                <InputText v-model="companyData.city" class="w-full" />
              </div>
            </div>
            <div class="field-group">
              <div class="field">
                <label>Utca</label>
                <InputText v-model="companyData.streetName" class="w-full" />
              </div>
              <div class="field">
                <label>Házszám</label>
                <InputText v-model="companyData.houseNumber" class="w-full" />
              </div>
            </div>
          </div>
        </TabPanel>

        <TabPanel header="Nyitvatartás">
          <div class="field">
            <label>Címsor (Pl: Bejelentkezés alapján)</label>
            <InputText v-model="companyData.openingHoursTitle" class="w-full" />
          </div>
          <div class="field">
            <label>Leírás (Pl: Jelenleg kizárólag...)</label>
            <Textarea v-model="companyData.openingHoursDescription" rows="2" class="w-full" />
          </div>
          <div class="field">
            <label>Időpontok (HTML engedélyezett, pl: &lt;br&gt;)</label>
            <Textarea v-model="companyData.openingTimeSlots" rows="4" class="w-full" />
            <small>Tipp: Használja a &lt;br&gt; kódot új sor kezdéséhez!</small>
          </div>
          <div class="field">
            <label>Extra infó (Pl: Facebookon tesszük közzé...)</label>
            <Textarea v-model="companyData.openingExtraInfo" rows="2" class="w-full" />
          </div>
        </TabPanel>

        <TabPanel header="Közösségi & Térkép">
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
        </TabPanel>

        <TabPanel header="Megjelenés">
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
        </TabPanel>

        <TabPanel header="Fordítások & Nyelvek">
          <div class="p-3">
            <LanguageManager />
            <hr class="separator" style="margin: 3rem 0;" />
            <UiTranslationManager />
          </div>
        </TabPanel>

      </TabView>

      <div class="actions">
        <Button :label="isSaving ? 'Mentés folyamatban...' : 'Beállítások Mentése'"
                icon="pi pi-check"
                :disabled="isSaving"
                @click="saveSettings"
                class="save-btn" />
      </div>
    </div>
  </div>
</template>

<style scoped>
  /* A stílusok ugyanazok, mint eddig, csak a p-3 helper kellhet */
  .p-3 {
    padding: 1rem;
  }

  .settings-container {
    max-width: 900px;
    margin: 0 auto;
    padding: 2rem;
  }

  h1 {
    color: var(--primary-color);
  }

  h3 {
    margin-top: 1.5rem;
    margin-bottom: 0.5rem;
    color: #555;
  }

  .intro {
    margin-bottom: 2rem;
    color: #666;
  }

  .form-wrapper {
    background: white;
    padding: 1rem;
    border-radius: 8px;
    box-shadow: 0 2px 10px rgba(0,0,0,0.1);
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

  :deep(.p-inputtext), :deep(.p-inputtextarea) {
    width: 100%;
  }

  .w-full {
    width: 100%;
  }

  .field-group {
    display: flex;
    gap: 1rem;
  }

    .field-group .field {
      flex: 1;
    }

  .alert-box {
    padding: 1rem;
    border-radius: 4px;
    margin-bottom: 1rem;
    text-align: center;
    font-weight: bold;
  }

  .success {
    background: #d4edda;
    color: #155724;
    border: 1px solid #c3e6cb;
  }

  .error {
    background: #f8d7da;
    color: #721c24;
    border: 1px solid #f5c6cb;
  }

  .loading {
    background: #e2e3e5;
    color: #383d41;
  }

  .actions {
    margin-top: 2rem;
    text-align: right;
  }

  .save-btn {
    background-color: var(--primary-color) !important;
    border: none !important;
    padding: 10px 20px;
  }

    .save-btn:hover {
      filter: brightness(90%);
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
