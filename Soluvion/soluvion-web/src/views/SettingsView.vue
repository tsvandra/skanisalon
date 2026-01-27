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

const companyData = ref({});
const isLoading = ref(true);
const successMsg = ref('');
const errorMsg = ref('');

// Token a mentéshez
const token = localStorage.getItem('salon_token');

// Betöltés (A már meglévő API-ból, de most a sajátunkat kérjük)
const loadSettings = async () => {
  // Mivel a CompanyController GetById-t használ, és most még fixen 1-esek vagyunk a kliens oldalon,
  // ideiglenesen lekérjük az 1-est. (Később a /api/Company/me végpont lenne a szép).
  // De a PUT már a tokenből dolgozik!
  try {
    const res = await fetch('https://localhost:7113/api/Company/1'); // Portszámot ellenőrizd!
    if (res.ok) {
      companyData.value = await res.json();
    }
  } catch (err) {
    console.error(err);
  } finally {
    isLoading.value = false;
  }
};

// Mentés
const saveSettings = async () => {
  successMsg.value = '';
  errorMsg.value = '';

  try {
    const res = await fetch('https://localhost:7113/api/Company', { // PUT hívás
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${token}` // KÖTELEZŐ: Ez azonosítja a céget
      },
      body: JSON.stringify(companyData.value)
    });

    if (res.ok) {
      successMsg.value = 'A beállítások sikeresen mentve!';
      // Frissítjük a CSS változókat azonnal, hogy lássa az eredményt
      document.documentElement.style.setProperty('--primary-color', companyData.value.primaryColor);
      document.documentElement.style.setProperty('--secondary-color', companyData.value.secondaryColor);

      // 3 mp múlva eltüntetjük az üzenetet
      setTimeout(() => successMsg.value = '', 3000);
    } else {
      errorMsg.value = 'Hiba a mentés során. Lehet, hogy lejárt a belépésed?';
    }
  } catch (err) {
    errorMsg.value = 'Hálózati hiba történt.';
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
