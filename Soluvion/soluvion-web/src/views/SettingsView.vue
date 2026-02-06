<script setup>
  import { ref, onMounted, inject } from 'vue';
  import Card from 'primevue/card';
  import InputText from 'primevue/inputtext';
  import Textarea from 'primevue/textarea';
  import Button from 'primevue/button';
  import Message from 'primevue/message';
  import TabView from 'primevue/tabview';
  import TabPanel from 'primevue/tabpanel';
  import ColorPicker from 'primevue/colorpicker'; // Ha van PrimeVue ColorPicker
  import api from '@/services/api';

  const companyData = ref({});
  const isLoading = ref(false);
  const isSaving = ref(false);
  const successMsg = ref('');
  const errorMsg = ref('');

  // Az űrlap adatai (ezeket fogjuk szerkeszteni)
  const form = ref({
    name: '',
    email: '',
    phone: '',
    address: '',
    primaryColor: '#d4af37',
    secondaryColor: '#1a1a1a',
    logoUrl: ''
  });

  // Token a mentéshez
  const token = localStorage.getItem('salon_token');

  // Segédfüggvény: Token dekódolása (ugyanaz, mint az App.vue-ban)
  const getCompanyIdFromToken = () => {
    try {
      const token = localStorage.getItem('salon_token');
      if (!token) return null;
      const decoded = JSON.parse(atob(token.split('.')[1]));
      return decoded?.CompanyId || decoded?.companyId;
    } catch (e) {
      return null;
    }
  };

  // 1. Adatok betöltése
  const loadCompanyData = async () => {
    const companyId = getCompanyIdFromToken();
    if (!companyId) {
      errorMsg.value = "Nem található cégazonosító. Kérjük, jelentkezz be újra!";
      return;
    }

    isLoading.value = true;
    try {
      // JAVÍTÁS: Nem 'getCompanyDetails'-t hívunk, hanem sima api.get-et!
      const res = await api.get(`/api/Company/${companyId}`);

      // Feltöltjük az űrlapot a kapott adatokkal
      form.value = { ...res.data };

    } catch (err) {
      console.error("Hiba a betöltéskor:", err);
      errorMsg.value = "Nem sikerült betölteni a cég adatait.";
    } finally {
      isLoading.value = false;
    }
  };

  // Betöltés (A már meglévő API-ból, de most a sajátunkat kérjük)
  const loadSettings = async () => {
    try {
      // ID javítása 7-re
      const res = await api.get('/api/Company/7');
      companyData.value = res.data;
    } catch (err) {
      console.error(err);
    } finally {
      isLoading.value = false;
    }
  };

  //// Mentés
  //const saveSettings = async () => {
  //  successMsg.value = '';
  //  errorMsg.value = '';

  //  try {
  //    // A PUT kérés, token automatikusan megy
  //    await api.put('/api/Company', companyData.value);

  //    successMsg.value = 'A beállítások sikeresen mentve!';
  //    document.documentElement.style.setProperty('--primary-color', companyData.value.primaryColor);
  //    document.documentElement.style.setProperty('--secondary-color', companyData.value.secondaryColor);
  //    setTimeout(() => successMsg.value = '', 3000);

  //  } catch (err) {
  //    errorMsg.value = 'Hiba a mentés során.';
  //  }
  //};

  // 2. Mentés (PUT kérés)
  const saveSettings = async () => {
    const companyId = getCompanyIdFromToken();
    if (!companyId) return;

    isSaving.value = true;
    message.value = '';
    errorMsg.value = '';

    try {
      // JAVÍTÁS: A módosított adatokat (form.value) küldjük vissza
      await api.put(`/api/Company/${companyId}`, form.value);

      message.value = "A változtatások sikeresen mentve!";

      // Frissítjük a CSS változókat azonnal, hogy lásd a színváltozást
      document.documentElement.style.setProperty('--primary-color', form.value.primaryColor);
      document.documentElement.style.setProperty('--secondary-color', form.value.secondaryColor);

      // Opcionális: Újratöltjük az oldalt kis késleltetéssel, hogy a Header is frissüljön
      setTimeout(() => window.location.reload(), 1500);

    } catch (err) {
      console.error("Mentési hiba:", err);
      errorMsg.value = err.response?.data || "Hiba történt a mentés során.";
    } finally {
      isSaving.value = false;
    }
  };

  onMounted(() => {
    loadSettings();
  });
</script>

<template>
  <div class="settings-container">
    <h1>Cégbeállítások</h1>
    <p class="intro">Itt módosíthatja a weboldalán megjelenő adatokat.</p>

    <div v-if="successMsg" class="alert-box success">{{ successMsg }}</div>
    <div v-if="errorMsg" class="alert-box error">{{ errorMsg }}</div>

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
                <InputText v-model="companyData.postalCode" />
              </div>
              <div class="field">
                <label>Város</label>
                <InputText v-model="companyData.city" />
              </div>
            </div>
            <div class="field-group">
              <div class="field">
                <label>Utca</label>
                <InputText v-model="companyData.streetName" />
              </div>
              <div class="field">
                <label>Házszám</label>
                <InputText v-model="companyData.houseNumber" />
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
          <p>Figyelem: A színek megváltoztatása azonnal hatással van az oldalra!</p>
          <div class="color-grid">
            <div class="field">
              <label>Elsődleges Szín (Gombok, Ikonok)</label>
              <div class="color-input-wrapper">
                <input type="color" v-model="companyData.primaryColor" />
                <span>{{ companyData.primaryColor }}</span>
              </div>
            </div>
            <div class="field">
              <label>Másodlagos Szín (Háttér)</label>
              <div class="color-input-wrapper">
                <input type="color" v-model="companyData.secondaryColor" />
                <span>{{ companyData.secondaryColor }}</span>
              </div>
            </div>
          </div>
        </TabPanel>

      </TabView>

      <div class="actions">
        <Button label="Beállítások Mentése" icon="pi pi-check" @click="saveSettings" class="save-btn" />
      </div>

    </div>
  </div>
</template>

<style scoped>
  .settings-container {
    max-width: 900px;
    margin: 0 auto;
    padding: 2rem;
  }

  h1 {
    color: var(--primary-color);
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

  .actions {
    margin-top: 2rem;
    text-align: right;
  }

  .save-btn {
    background-color: var(--primary-color);
    border: none;
    padding: 10px 20px;
  }

    .save-btn:hover {
      background-color: #b5952f;
    }

  /* Color picker stílus */
  .color-input-wrapper {
    display: flex;
    align-items: center;
    gap: 10px;
  }

  input[type="color"] {
    width: 50px;
    height: 50px;
    border: none;
    cursor: pointer;
  }
</style>
