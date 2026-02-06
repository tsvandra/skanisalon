<script setup>
  import { ref, onMounted } from 'vue';
  // PrimeVue komponensek importálása (Feltételezem, hogy regisztrálva vannak globálisan vagy itt kell)
  import InputText from 'primevue/inputtext';
  import Textarea from 'primevue/textarea';
  import Button from 'primevue/button';
  import TabView from 'primevue/tabview';
  import TabPanel from 'primevue/tabpanel';
  import api from '@/services/api';

  // Ezt használja a felület (egyesítettük a form-al)
  const companyData = ref({
    name: '',
    email: '',
    phone: '',
    postalCode: '',
    city: '',
    streetName: '',
    houseNumber: '',
    openingHoursTitle: '',
    openingHoursDescription: '',
    openingTimeSlots: '',
    openingExtraInfo: '',
    facebookUrl: '',
    instagramUrl: '',
    tikTokUrl: '',
    mapEmbedUrl: '',
    primaryColor: '#d4af37',
    secondaryColor: '#1a1a1a',
    logoUrl: ''
  });

  const isLoading = ref(false);
  const isSaving = ref(false);
  const successMsg = ref('');
  const errorMsg = ref('');

  // 1. Token dekódolása (Hogy tudjuk, melyik céget kell szerkeszteni)
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

  // 2. Adatok betöltése
  const loadCompanyData = async () => {
    const companyId = getCompanyIdFromToken();
    if (!companyId) {
      errorMsg.value = "Nem található cégazonosító. Kérjük, jelentkezz be újra!";
      return;
    }

    isLoading.value = true;
    try {
      // API hívás a dinamikus ID-val
      const res = await api.get(`/api/Company/${companyId}`);

      // JAVÍTÁS: A felület változóját töltjük fel az adatokkal!
      // Spread operatorral (...) másoljuk, hogy biztonságos legyen
      companyData.value = { ...res.data };

    } catch (err) {
      console.error("Hiba a betöltéskor:", err);
      errorMsg.value = "Nem sikerült betölteni a cég adatait.";
    } finally {
      isLoading.value = false;
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
      // JAVÍTÁS: A 'companyData'-t küldjük vissza, amit a felületen szerkesztettél
      await api.put(`/api/Company/${companyId}`, companyData.value);

      successMsg.value = "A változtatások sikeresen mentve!";

      // Azonnali színfrissítés, hogy lásd az eredményt
      if (companyData.value.primaryColor) {
        document.documentElement.style.setProperty('--primary-color', companyData.value.primaryColor);
        document.documentElement.style.setProperty('--secondary-color', companyData.value.secondaryColor);
      }

      // Kis idő múlva eltüntetjük az üzenetet
      setTimeout(() => successMsg.value = '', 3000);
      // Opcionális: oldal frissítése, ha a fejlécben a nevet is frissíteni akarod
      // setTimeout(() => window.location.reload(), 1500);

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
    <p class="intro">Itt módosíthatja a weboldalán megjelenő adatokat.</p>

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
