<script setup>
  import { ref, onMounted } from 'vue';
  import Button from 'primevue/button';
  import Tabs from 'primevue/tabs';
  import TabList from 'primevue/tablist';
  import Tab from 'primevue/tab';
  import TabPanels from 'primevue/tabpanels';
  import TabPanel from 'primevue/tabpanel';

  import api from '@/services/api';
  import { getCompanyIdFromToken } from '@/utils/jwt';

  import SettingsOpeningHours from '@/components/admin/settings/SettingsOpeningHours.vue';
  import SettingsContact from '@/components/admin/settings/SettingsContact.vue';
  import SettingsSocial from '@/components/admin/settings/SettingsSocial.vue';
  import SettingsAppearance from '@/components/admin/settings/SettingsAppearance.vue';
  import SettingsTranslations from '@/components/admin/settings/SettingsTranslations.vue';

  const companyData = ref({});
  const isLoading = ref(false);
  const isSaving = ref(false);
  const successMsg = ref('');
  const errorMsg = ref('');

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

    <div v-if="!isLoading && companyData" class="form-wrapper">

      <Tabs value="0">
        <TabList>
          <Tab value="0">Elérhetőségek</Tab>
          <Tab value="1">Nyitvatartás</Tab>
          <Tab value="2">Közösségi & Térkép</Tab>
          <Tab value="3">Megjelenés</Tab>
          <Tab value="4">Fordítások & Nyelvek</Tab>
        </TabList>

        <TabPanels>

          <TabPanel value="0">
            <SettingsContact :companyData="companyData" />
          </TabPanel>

          <TabPanel value="1">
            <SettingsOpeningHours :companyData="companyData" />
          </TabPanel>

          <TabPanel value="2">
            <SettingsSocial :companyData="companyData" />
          </TabPanel>

          <TabPanel value="3">
            <SettingsAppearance :companyData="companyData" />
          </TabPanel>

          <TabPanel value="4">
            <SettingsTranslations />
          </TabPanel>

        </TabPanels>
      </Tabs>

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

  .separator {
    border: 0;
    border-top: 1px solid #eee;
    margin: 20px 0;
  }
</style>
