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
  <div class="max-w-[1000px] mx-auto p-4 md:p-8 text-text">

    <div class="mb-8">
      <h1 class="text-3xl font-light tracking-wide text-primary m-0 mb-2">Cégbeállítások</h1>
      <p class="text-text-muted m-0">Itt módosíthatja a weboldalán megjelenő adatokat és a dizájnt.</p>
    </div>

    <div v-if="successMsg" class="p-4 rounded-lg mb-6 text-center font-bold bg-green-500/10 text-green-400 border border-green-500/30 flex items-center justify-center gap-2 shadow-sm">
      <i class="pi pi-check-circle"></i> {{ successMsg }}
    </div>

    <div v-if="errorMsg" class="p-4 rounded-lg mb-6 text-center font-bold bg-red-500/10 text-red-400 border border-red-500/30 flex items-center justify-center gap-2 shadow-sm">
      <i class="pi pi-times-circle"></i> {{ errorMsg }}
    </div>

    <div v-if="isLoading" class="p-4 rounded-lg mb-6 text-center font-bold bg-text/5 text-text-muted border border-text/10 flex items-center justify-center gap-2 shadow-sm">
      <i class="pi pi-spin pi-spinner"></i> Adatok betöltése...
    </div>

    <div v-if="!isLoading && companyData" class="bg-text/5 border border-text/10 p-4 md:p-6 rounded-2xl shadow-xl backdrop-blur-sm">

      <Tabs value="0" class="w-full
        [&_.p-tablist]:bg-transparent
        [&_.p-tablist-tab-list]:bg-transparent [&_.p-tablist-tab-list]:border-b [&_.p-tablist-tab-list]:border-text/20
        [&_.p-tab]:bg-transparent [&_.p-tab]:text-text-muted [&_.p-tab]:font-medium [&_.p-tab]:transition-all [&_.p-tab]:px-4 [&_.p-tab]:py-3
        [&_.p-tab:hover]:text-text [&_.p-tab:hover]:bg-text/5 [&_.p-tab:hover]:border-text/30
        [&_.p-tab-active]:!text-primary [&_.p-tab-active]:!border-primary [&_.p-tab-active]:!bg-primary/10
        [&_.p-tabpanels]:bg-transparent [&_.p-tabpanels]:text-text [&_.p-tabpanels]:p-0 [&_.p-tabpanels]:pt-6">

        <TabList class="mb-2">
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

      <div class="mt-10 flex justify-end border-t border-text/10 pt-6">
        <Button :label="isSaving ? 'Mentés folyamatban...' : 'Beállítások Mentése'"
                :icon="isSaving ? 'pi pi-spin pi-spinner' : 'pi pi-check'"
                :disabled="isSaving"
                @click="saveSettings"
                class="!bg-primary !text-black font-bold !border-none !px-8 !py-3.5 !rounded-xl hover:!scale-105 transition-transform shadow-md" />
      </div>

    </div>
  </div>
</template>
